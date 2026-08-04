using Gaffer.Api.Infrastructure;
using Gaffer.Api.Infrastructure.Databases;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public class DatabaseConfig {
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; }
}

public static class DatabaseStartup
{
    public static void ConfigureDatabase(
        this IServiceCollection services, 
        IConfiguration configuration) 
    {
        var infrastructureOptions = configuration.GetSection("DatabaseConfig");
        services.Configure<DatabaseConfig>(infrastructureOptions);
        var dbConfig = infrastructureOptions.Get<DatabaseConfig>();
        if (dbConfig is not { Enabled: true } || string.IsNullOrEmpty(dbConfig.ConnectionString)) {
            AddDummyConnector(services);
            return;
        }

        var sqlConnectionProvider = new SqlConnectionProvider(dbConfig.ConnectionString);
        if (!ConnectionIsUp(sqlConnectionProvider)) {
            AddDummyConnector(services);
            return;
        }

        services.AddScoped<IDatabaseConnectionProvider>(_ => sqlConnectionProvider);
    }

    private static void AddDummyConnector(IServiceCollection services) 
        => services.AddScoped<IDatabaseConnectionProvider, DummyConnectionProvider>();

    private static bool ConnectionIsUp(SqlConnectionProvider sqlConnectionProvider)
        => sqlConnectionProvider.TestConnection().Result;
}