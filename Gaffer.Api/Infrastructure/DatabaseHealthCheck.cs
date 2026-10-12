using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Startups;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gaffer.Api.Infrastructure;

/// <summary>Checks whether the configured database answers. Only registered when the SQL provider was chosen.</summary>
public interface IDatabaseProbe
{
    Task<bool> IsReachableAsync(CancellationToken cancellationToken);
}

public sealed class SqlDatabaseProbe(string connectionString) : IDatabaseProbe
{
    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
    {
        await using var provider = new SqlConnectionProvider(connectionString);
        return await provider.TestConnection(cancellationToken);
    }
}

/// <summary>
/// Readiness of the database. It relies on why the provider was chosen at startup, not on
/// <see cref="IDatabaseConnectionProvider.TestConnection"/> (the dummy provider always answers <c>true</c>):
/// a fallback dummy is <see cref="HealthStatus.Unhealthy"/> even if the database answers again (no recovery
/// without a restart), an intended dummy is <see cref="HealthStatus.Degraded"/>, and the SQL provider is probed
/// for at most <see cref="DefaultTimeout"/>. Results carry no exception, data or connection details.
/// </summary>
public sealed class DatabaseHealthCheck(
    DatabaseProviderSelection selection,
    IDatabaseProbe? probe,
    TimeSpan? timeout = null) : IHealthCheck
{
    public const string Name = "database";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly TimeSpan _timeout = timeout ?? DefaultTimeout;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        switch (selection.Reason)
        {
            case DatabaseProviderReason.DummyByConfiguration:
                return HealthCheckResult.Degraded("Database disabled by configuration.");
            case DatabaseProviderReason.DummyByFallback:
                return HealthCheckResult.Unhealthy("Database unreachable at startup; restart required.");
        }

        if (probe is null) return HealthCheckResult.Unhealthy("Database probe not available.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout);
        try
        {
            // WaitAsync bounds the wait even if the probe ignores its token.
            var reachable = await probe.IsReachableAsync(deadline.Token).WaitAsync(deadline.Token);
            return reachable ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database not reachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database check timed out.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database check failed.");
        }
    }
}
