using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Startups;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gaffer.Api.Infrastructure;

public interface IDatabaseProbe {
    Task<bool> IsReachableAsync(CancellationToken cancellationToken);
}

public sealed class SqlDatabaseProbe(string connectionString) : IDatabaseProbe {
    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken) {
        await using var provider = new SqlConnectionProvider(connectionString);
        return await provider.TestConnection(cancellationToken);
    }
}

public sealed class DatabaseHealthCheck(
    DatabaseProviderSelection selection,
    IDatabaseProbe? probe,
    TimeSpan? timeout = null,
    ILogger<DatabaseHealthCheck>? logger = null) : IHealthCheck {
    public const string Name = "database";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly TimeSpan timeout = timeout ?? DefaultTimeout;
    private readonly ILogger logger = logger ?? NullLogger<DatabaseHealthCheck>.Instance;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        switch (selection.Reason) {
            case DatabaseProviderReason.DummyByConfiguration:
                return HealthCheckResult.Degraded("Database disabled by configuration.");
            case DatabaseProviderReason.DummyByFallback:
                return HealthCheckResult.Unhealthy("Database unreachable at startup; restart required.");
        }

        if (probe is null) return HealthCheckResult.Unhealthy("Database probe not available.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try {
            var reachable = await probe.IsReachableAsync(deadline.Token).WaitAsync(deadline.Token);
            if (reachable) return HealthCheckResult.Healthy();

            logger.LogWarning("Database health check failed: the database did not answer.");
            return HealthCheckResult.Unhealthy("Database not reachable.");
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            logger.LogWarning("Database health check timed out after {Timeout}.", timeout);
            return HealthCheckResult.Unhealthy("Database check timed out.");
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            logger.LogWarning(exception, "Database health check failed with an exception.");
            return HealthCheckResult.Unhealthy("Database check failed.");
        }
    }
}
