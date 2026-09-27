using AwesomeAssertions;
using Gaffer.Api.Shared.ConnectionProviders;

namespace Gaffer.Api.Tests.Unit.Repositories;

/// <summary>
/// Test double for <see cref="IDatabaseConnectionProvider"/>: records the token of every call and delegates to
/// <paramref name="inner"/>, so a repository method runs every query it would normally run (for example the address
/// and stands lookups of each facility row) and every call can be checked.
/// </summary>
internal sealed class TokenRecordingConnectionProvider(IDatabaseConnectionProvider inner) : IDatabaseConnectionProvider
{
    private readonly List<(string Call, CancellationToken Token)> _calls = [];

    public Task<bool> TestConnection() => inner.TestConnection();

    public Task ClearAsync(List<string> tablesToDelete) => inner.ClearAsync(tablesToDelete);

    public Task<T> QueryScalarAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Record(nameof(QueryScalarAsync), query, cancellationToken);
        return inner.QueryScalarAsync<T>(query, parameters, cancellationToken);
    }

    public Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Record(nameof(QueryAsync), query, cancellationToken);
        return inner.QueryAsync<T>(query, parameters, cancellationToken);
    }

    public Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Record(nameof(ExecuteAsync), query, cancellationToken);
        return inner.ExecuteAsync(query, parameters, cancellationToken);
    }

    public Task ExecuteInTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
    {
        Record(nameof(ExecuteInTransactionAsync), string.Empty, cancellationToken);
        return inner.ExecuteInTransactionAsync(operations, cancellationToken);
    }

    /// <summary>Asserts that at least <paramref name="minimumCalls"/> calls were made and all of them received <paramref name="expected"/>.</summary>
    public void ShouldHaveReceivedOnly(CancellationToken expected, int minimumCalls)
    {
        _calls.Count.Should().BeGreaterThanOrEqualTo(minimumCalls);
        _calls.Where(call => call.Token != expected).Select(call => call.Call)
            .Should().BeEmpty("every provider call must receive the repository's token");
    }

    private void Record(string method, string query, CancellationToken token)
        => _calls.Add(($"{method} {query}".TrimEnd(), token));
}
