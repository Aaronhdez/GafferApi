namespace Gaffer.Api.Shared.ConnectionProviders;

public interface IDatabaseConnectionProvider {
    public Task<bool> TestConnection();
    Task ClearAsync(List<string> tablesToDelete);
    Task<T> QueryScalarAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operations"/> atomically: commits on success, rolls back and rethrows on failure.
    /// <paramref name="cancellationToken"/> cancels opening the connection and beginning the transaction; the
    /// operations pass their own token to each call.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);
}
