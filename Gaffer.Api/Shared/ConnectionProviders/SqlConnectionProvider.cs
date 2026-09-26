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
    {
        if (_sqlConnection.State != ConnectionState.Open)
        {
            _sqlConnection.Open();
        }
        foreach (var table in tablesToDelete)
        {
            await _sqlConnection.ExecuteAsync($"DELETE FROM `{table}`");
        }
        if (_sqlConnection.State == ConnectionState.Open)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
    }

    public async Task CreateAsync(string database)
    {
        if (_sqlConnection.State != ConnectionState.Open)
        {
            _sqlConnection.Open();
        }
        var result = await _sqlConnection.ExecuteAsync("CREATE Database if not exists @database", new { database });
        if (_sqlConnection.State == ConnectionState.Open)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
    }
    
    public async Task<T> QueryScalarAsync<T>(string query, object? parameters = null)
    {
        var ownsConnection = await OpenIfNeededAsync();
        var result = await _sqlConnection.QueryFirstOrDefaultAsync<T>(query, parameters, _currentTransaction);
        if (ownsConnection)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
        return result;
    }

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null)
    {
        var ownsConnection = await OpenIfNeededAsync();
        var result = await _sqlConnection.QueryAsync<T>(query, parameters, _currentTransaction);
        if (ownsConnection)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
        return result.ToList();
    }

    public async Task ExecuteAsync(string query, object? parameters = null)
    {
        var ownsConnection = await OpenIfNeededAsync();
        await _sqlConnection.ExecuteAsync(query, parameters, _currentTransaction);
        if (ownsConnection)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
    }

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
                await _sqlConnection.DisposeAsync();
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
        await _sqlConnection.DisposeAsync();
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        _currentTransaction?.Dispose();
        _sqlConnection.Close();
        _sqlConnection.Dispose();
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