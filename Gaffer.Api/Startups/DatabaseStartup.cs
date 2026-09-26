using Gaffer.Api.Shared.ConnectionProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gaffer.Api.Startups;

public class DatabaseConfig {
    public bool Enabled { get; set; }
    public string? ConnectionString { get; set; }
}

public static class DatabaseStartup
{
    public static void ConfigureDatabase(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILogger? logger = null)
    {
        var infrastructureOptions = configuration.GetSection("DatabaseConfig");
        services.Configure<DatabaseConfig>(infrastructureOptions);
        var dbConfig = infrastructureOptions.Get<DatabaseConfig>();
        
        if (dbConfig is not { Enabled: true } || string.IsNullOrEmpty(dbConfig.ConnectionString)) {
            AddDummyConnector(services);
            return;
        }
        
        var connectionString = dbConfig.ConnectionString;
        if (!ConnectionIsUp(connectionString)) {
            logger?.LogWarning(
                "Database is not reachable at startup; falling back to {ConnectionProvider}.",
                nameof(DummyConnectionProvider));
            AddDummyConnector(services);
            return;
        }

        services.AddScoped<IDatabaseConnectionProvider>(_ => new SqlConnectionProvider(connectionString));
    }

    private static void AddDummyConnector(IServiceCollection services)
        => services.AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>();

    // ConfigureServices is synchronous. The async probe runs on the thread pool so no caller
    // synchronization context can deadlock it, and GetResult() rethrows without AggregateException.
    private static bool ConnectionIsUp(string connectionString)
    {
        using var probe = new SqlConnectionProvider(connectionString);
        return Task.Run(probe.TestConnection).GetAwaiter().GetResult();
    }
}