namespace Gaffer.Api.Shared.ConnectionProviders;

public class DummyConnectionProvider : IDatabaseConnectionProvider
{
    public Task<bool> TestConnection() => Task.FromResult(true);
    public Task ClearAsync(string database, List<string> tablesToDelete)
    {
        throw new NotImplementedException();
    }

    public Task<T> QueryScalarAsync<T>(string query, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> QueryAsync<T>(string query, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task ExecuteAsync(string query, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task CreateAsync(string database)
    {
        throw new NotImplementedException();
    }
}