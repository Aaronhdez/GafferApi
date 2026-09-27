using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using Dapper;
using MySql.Data.MySqlClient;

namespace Gaffer.Api.Shared.ConnectionProviders;

public class SqlConnectionProvider : IDatabaseConnectionProvider, IAsyncDisposable, IDisposable
{
    private readonly MySqlConnection _sqlConnection;
    private MySqlTransaction? _currentTransaction;

    public bool IsDisposed { get; private set; }
    public ConnectionState ConnectionState => _sqlConnection.State;

    public SqlConnectionProvider(string? dbConnection)
    {
        if (string.IsNullOrEmpty(dbConnection)) throw new ArgumentNullException(nameof(dbConnection));
        _sqlConnection = new MySqlConnection(dbConnection);
    }

    /// <summary>Checks connectivity with a schema-independent query. Connectivity failures yield <c>false</c> instead of throwing.</summary>
    public async Task<bool> TestConnection()
    {
        var ownsConnection = _sqlConnection.State != ConnectionState.Open;
        try
        {
            if (ownsConnection)
            {
                await _sqlConnection.OpenAsync();
            }
            await using var command = new MySqlCommand("SELECT 1;", _sqlConnection);
            await command.ExecuteScalarAsync();
            return true;
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or SocketException)
        {
            return false;
        }
        finally
        {
            if (ownsConnection)
            {
                await _sqlConnection.CloseAsync();
            }
        }
    }

    public async Task ClearAsync(List<string> tablesToDelete)
        => await RunOnConnectionAsync(async () =>
        {
            foreach (var table in tablesToDelete)
            {
                await _sqlConnection.ExecuteAsync($"DELETE FROM `{table}`");
            }
            return true;
        });

    public async Task CreateAsync(string database)
        => await RunOnConnectionAsync(() =>
            _sqlConnection.ExecuteAsync("CREATE Database if not exists @database", new { database }));

    public Task<T> QueryScalarAsync<T>(string query, object? parameters = null)
        => RunOnConnectionAsync(async () =>
            (await _sqlConnection.QueryFirstOrDefaultAsync<T>(query, parameters, _currentTransaction))!);

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null)
        => (await RunOnConnectionAsync(() =>
            _sqlConnection.QueryAsync<T>(query, parameters, _currentTransaction))).ToList();

    public async Task ExecuteAsync(string query, object? parameters = null)
        => await RunOnConnectionAsync(() =>
            _sqlConnection.ExecuteAsync(query, parameters, _currentTransaction));

    public async Task ExecuteInTransactionAsync(Func<Task> operations)
    {
        if (_currentTransaction != null)
        {
            // Already running inside an ambient transaction started by an outer call: just execute inline.
            await operations();
            return;
        }

        await OpenIfNeededAsync();
        _currentTransaction = await _sqlConnection.BeginTransactionAsync();
        try
        {
            await operations();
            await _currentTransaction.CommitAsync();
        }
        catch
        {
            await _currentTransaction.RollbackAsync();
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            if (_sqlConnection.State == ConnectionState.Open)
            {
                await _sqlConnection.CloseAsync();
            }
        }
    }

    /// <summary>Closes and releases the connection (and any pending transaction) when the owning DI scope ends. Idempotent.</summary>
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
        }
        await _sqlConnection.CloseAsync();
        // MySql.Data's DisposeAsync does not run Component disposal; after CloseAsync the synchronous Dispose does no I/O.
        _sqlConnection.Dispose();
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        _currentTransaction?.Dispose();
        _sqlConnection.Close();
        _sqlConnection.Dispose();
    }

    /// <summary>
    /// Runs <paramref name="operation"/> on the connection, opening it if needed and closing it (back to the pool,
    /// without releasing it) when this call opened it. Inside an ambient transaction the transaction owns the connection.
    /// A failing operation is not closed here (TECHDEBT-003 Q1 debt): the connection stays open until the next
    /// transaction ends or the provider is disposed with its scope.
    /// </summary>
    private async Task<T> RunOnConnectionAsync<T>(Func<Task<T>> operation)
    {
        var ownsConnection = await OpenIfNeededAsync();
        var result = await operation();
        if (ownsConnection)
        {
            await _sqlConnection.CloseAsync();
        }
        return result;
    }

    /// <summary>Opens the connection if not already open. Returns whether the caller is responsible for closing it.</summary>
    private async Task<bool> OpenIfNeededAsync()
    {
        if (_currentTransaction != null) return false;

        if (_sqlConnection.State != ConnectionState.Open)
        {
            await _sqlConnection.OpenAsync();
        }
        return true;
    }
}
