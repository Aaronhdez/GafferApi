namespace Gaffer.Api.Shared.ConnectionProviders;

public class DummyConnectionProvider : IDatabaseConnectionProvider {
    public Task<bool> TestConnection() => Task.FromResult(true);
    public Task ClearAsync(List<string> tablesToDelete) => throw new NotImplementedException();

    public Task<T> QueryScalarAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task CreateAsync(string database) => throw new NotImplementedException();
}
