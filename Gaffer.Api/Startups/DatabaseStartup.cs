using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Gaffer.Api.Startups;

public class DatabaseConfig {
    public bool Enabled { get; set; }
    public string? ConnectionString { get; set; }
}

public enum DatabaseProviderReason {
    Sql,
    DummyByConfiguration,
    DummyByFallback
}

public sealed record DatabaseProviderSelection(DatabaseProviderReason Reason);

public static class DatabaseStartup {
    public const string ReadyTag = "ready";

    public static void ConfigureDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger? logger = null) {
        var infrastructureOptions = configuration.GetSection("DatabaseConfig");
        services.Configure<DatabaseConfig>(infrastructureOptions);
        var dbConfig = infrastructureOptions.Get<DatabaseConfig>();
        AddDatabaseHealthCheck(services);

        if (dbConfig is not { Enabled: true } || string.IsNullOrEmpty(dbConfig.ConnectionString)) {
            AddDummyConnector(services, DatabaseProviderReason.DummyByConfiguration);
            return;
        }

        var connectionString = dbConfig.ConnectionString;
        if (!ConnectionIsUp(connectionString)) {
            logger?.LogWarning(
                "Database is not reachable at startup; falling back to {ConnectionProvider}.",
                nameof(DummyConnectionProvider));
            AddDummyConnector(services, DatabaseProviderReason.DummyByFallback);
            return;
        }

        services.AddScoped<IDatabaseConnectionProvider>(_ => new SqlConnectionProvider(connectionString));
        services.AddSingleton(new DatabaseProviderSelection(DatabaseProviderReason.Sql));
        services.AddSingleton<IDatabaseProbe>(new SqlDatabaseProbe(connectionString));
    }

    private static void AddDummyConnector(IServiceCollection services, DatabaseProviderReason reason) {
        services.AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>();
        services.AddSingleton(new DatabaseProviderSelection(reason));
    }

    private static void AddDatabaseHealthCheck(IServiceCollection services)
        => services.AddHealthChecks().Add(new HealthCheckRegistration(
            DatabaseHealthCheck.Name,
            serviceProvider => new DatabaseHealthCheck(
                serviceProvider.GetRequiredService<DatabaseProviderSelection>(),
                serviceProvider.GetService<IDatabaseProbe>(),
                logger: serviceProvider.GetService<ILogger<DatabaseHealthCheck>>()),
            failureStatus: null,
            tags: [ReadyTag]));

    private static bool ConnectionIsUp(string connectionString) {
        using var probe = new SqlConnectionProvider(connectionString);
        return Task.Run(probe.TestConnection).GetAwaiter().GetResult();
    }
}
