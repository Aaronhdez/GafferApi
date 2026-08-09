using System.Data;
using Dapper;
using MySql.Data.MySqlClient;

namespace Gaffer.Api.Shared.ConnectionProviders;

public class SqlConnectionProvider : IDatabaseConnectionProvider
{
    private readonly MySqlConnection _sqlConnection;

    public SqlConnectionProvider(string? dbConnection)
    {
        if (string.IsNullOrEmpty(dbConnection)) throw new ArgumentNullException(nameof(dbConnection));
        _sqlConnection = new MySqlConnection(dbConnection);
    }

    public async Task<bool> TestConnection()
    {
        _sqlConnection.Open();
        var query = "SELECT (0) FROM Parameters";
        var command = new MySqlCommand(query, _sqlConnection);
        await command.ExecuteReaderAsync();
        await _sqlConnection.CloseAsync();
        return true;
    }

    public async Task ClearAsync(string database, List<string> tablesToDelete)
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
        if (_sqlConnection.State != ConnectionState.Open)
        {
            _sqlConnection.Open();
        }
        var result = await _sqlConnection.QueryFirstOrDefaultAsync<T>(query, parameters);
        if (_sqlConnection.State == ConnectionState.Open)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
        return result;
    }

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null)
    {
        if (_sqlConnection.State != ConnectionState.Open)
        {
            _sqlConnection.Open();
        }
        var result = await _sqlConnection.QueryAsync<T>(query, parameters);
        if (_sqlConnection.State == ConnectionState.Open)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
        return result.ToList();
    }

    public async Task ExecuteAsync(string query, object? parameters = null)
    {
        if (_sqlConnection.State != ConnectionState.Open)
        {
            _sqlConnection.Open();
        }
        await _sqlConnection.ExecuteAsync(query, parameters);
        if (_sqlConnection.State == ConnectionState.Open)
        {
            await _sqlConnection.CloseAsync();
            await _sqlConnection.DisposeAsync();
        }
    }
}