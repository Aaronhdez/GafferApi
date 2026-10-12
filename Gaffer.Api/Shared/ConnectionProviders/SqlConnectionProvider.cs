using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using Dapper;
using MySqlConnector;

namespace Gaffer.Api.Shared.ConnectionProviders;

public class SqlConnectionProvider : IDatabaseConnectionProvider, IAsyncDisposable, IDisposable {
    private readonly MySqlConnection sqlConnection;
    private MySqlTransaction? currentTransaction;

    public bool IsDisposed { get; private set; }
    public ConnectionState ConnectionState => sqlConnection.State;

    public SqlConnectionProvider(string? dbConnection) {
        if (string.IsNullOrEmpty(dbConnection)) throw new ArgumentNullException(nameof(dbConnection));
        sqlConnection = new MySqlConnection(dbConnection);
    }

    /// <summary>Checks connectivity with a schema-independent query. Connectivity failures yield <c>false</c> instead of throwing.</summary>
    public Task<bool> TestConnection() => TestConnection(default);

    /// <summary>
    /// Same check bound to <paramref name="cancellationToken"/>: a cancelled token throws
    /// <see cref="OperationCanceledException"/> (before touching the connection when already cancelled).
    /// </summary>
    public async Task<bool> TestConnection(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var ownsConnection = sqlConnection.State != ConnectionState.Open;
        try {
            if (ownsConnection) {
                await sqlConnection.OpenAsync(cancellationToken);
            }
            await using var command = new MySqlCommand("SELECT 1;", sqlConnection);
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        } catch (Exception ex) when (ex is DbException or TimeoutException or SocketException) {
            return false;
        } finally {
            if (ownsConnection) {
                await sqlConnection.CloseAsync();
            }
        }
    }

    public async Task ClearAsync(List<string> tablesToDelete)
        => await RunOnConnectionAsync(async () => {
            foreach (var table in tablesToDelete) {
                await sqlConnection.ExecuteAsync($"DELETE FROM `{table}`");
            }
            return true;
        });

    public async Task CreateAsync(string database)
        => await RunOnConnectionAsync(() =>
            sqlConnection.ExecuteAsync("CREATE Database if not exists @database", new { database }));

    public Task<T> QueryScalarAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
        => RunOnConnectionAsync(async () =>
            (await sqlConnection.QueryFirstOrDefaultAsync<T>(Command(query, parameters, cancellationToken)))!,
            cancellationToken);

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
        => (await RunOnConnectionAsync(() =>
            sqlConnection.QueryAsync<T>(Command(query, parameters, cancellationToken)), cancellationToken)).ToList();

    public async Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
        => await RunOnConnectionAsync(() =>
            sqlConnection.ExecuteAsync(Command(query, parameters, cancellationToken)), cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default) {
        if (currentTransaction != null) {
            // Already running inside an ambient transaction started by an outer call: just execute inline.
            cancellationToken.ThrowIfCancellationRequested();
            await operations();
            return;
        }

        await OpenIfNeededAsync(cancellationToken);
        try {
            currentTransaction = await sqlConnection.BeginTransactionAsync(cancellationToken);
        } catch {
            await sqlConnection.CloseAsync();
            throw;
        }
        try {
            await operations();
            await currentTransaction.CommitAsync();
        } catch {
            await currentTransaction.RollbackAsync();
            throw;
        } finally {
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
            if (sqlConnection.State == ConnectionState.Open) {
                await sqlConnection.CloseAsync();
            }
        }
    }

    /// <summary>Closes and releases the connection (and any pending transaction) when the owning DI scope ends. Idempotent.</summary>
    public async ValueTask DisposeAsync() {
        if (IsDisposed) return;
        IsDisposed = true;
        if (currentTransaction != null) {
            await currentTransaction.DisposeAsync();
        }
        // MySqlConnector's DisposeAsync closes the connection asynchronously and raises Component.Disposed.
        await sqlConnection.DisposeAsync();
    }

    public void Dispose() {
        if (IsDisposed) return;
        IsDisposed = true;
        currentTransaction?.Dispose();
        sqlConnection.Close();
        sqlConnection.Dispose();
    }

    /// <summary>
    /// Runs <paramref name="operation"/> on the connection, opening it if needed and closing it (back to the pool,
    /// without releasing it) when this call opened it, also when the operation fails or is cancelled; the original
    /// exception propagates unwrapped. Inside an ambient transaction the transaction owns the connection and closes it.
    /// </summary>
    private async Task<T> RunOnConnectionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) {
        var ownsConnection = await OpenIfNeededAsync(cancellationToken);
        try {
            return await operation();
        } finally {
            if (ownsConnection) {
                await sqlConnection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Opens the connection if not already open. Returns whether the caller is responsible for closing it.
    /// An already cancelled <paramref name="cancellationToken"/> throws before touching the connection.
    /// </summary>
    private async Task<bool> OpenIfNeededAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (currentTransaction != null) return false;

        if (sqlConnection.State != ConnectionState.Open) {
            await sqlConnection.OpenAsync(cancellationToken);
        }
        return true;
    }

    /// <summary>
    /// Single way to build a Dapper command: bound to the ambient transaction (if any) and to the caller's token.
    /// Cancelling the token aborts a query already running on the server (MySqlConnector sends <c>KILL QUERY</c>).
    /// </summary>
    private CommandDefinition Command(string query, object? parameters, CancellationToken cancellationToken)
        => new(query, parameters, currentTransaction, cancellationToken: cancellationToken);
}
