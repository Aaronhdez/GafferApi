namespace Gaffer.Api.Shared.ConnectionProviders;

public interface IDatabaseConnectionProvider
{
    public Task<bool> TestConnection();
    Task ClearAsync(string database, List<string> tablesToDelete);
    Task<T> QueryScalarAsync<T>(string query, object? parameters = null);
    Task<List<T>> QueryAsync<T>(string query, object? parameters = null);
    Task ExecuteAsync(string query, object? parameters = null);
}