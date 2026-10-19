namespace Gaffer.Api.Shared.ConnectionProviders;

public interface IDatabaseConnectionProvider {
    public Task<bool> TestConnection();
    Task ClearAsync(List<string> tablesToDelete);
    Task<T> QueryScalarAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);
}
