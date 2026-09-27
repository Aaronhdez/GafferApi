using System.Collections;
using Azure.Identity;
using FluentMigrator;
using FluentMigrator.Runner;
using Gaffer.Api.Migrations.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Gaffer.Api.Migrations;

public class Program
{
    private const string ConnectionStringsSection = "ConnectionStrings";
    private const string TargetKey = "Target";
    private const int ConfigurationErrorExitCode = 2;

    public static int Main(string[] args)
    {
        Console.WriteLine("Gaffer Api Migrations");
        var configuration = BuildConfiguration(args);
        List<DatabaseSettings> databaseSettings;
        try
        {
            databaseSettings = ResolveDatabases(configuration);
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return ConfigurationErrorExitCode;
        }

        foreach (var databaseSetting in databaseSettings)
        {
            var serviceCollection = new ServiceCollection();
            ApplyMigrationsIn(databaseSetting.ConnectionString, databaseSetting.Name, serviceCollection);
            serviceCollection.Clear();
        }

        return 0;
    }

    // Precedence (last wins): User Secrets, environment variables, command line.
    private static IConfigurationRoot BuildConfiguration(string[] args) => new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    // Every non-empty ConnectionStrings:<DatabaseName> is migrated; --Target <DatabaseName> restricts the run to one.
    private static List<DatabaseSettings> ResolveDatabases(IConfiguration configuration)
    {
        var configured = configuration.GetSection(ConnectionStringsSection).GetChildren()
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => new DatabaseSettings { Name = entry.Key, ConnectionString = entry.Value! })
            .ToList();
        var target = configuration[TargetKey];
        if (!string.IsNullOrWhiteSpace(target))
        {
            configured = configured
                .Where(database => string.Equals(database.Name, target, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (configured.Count > 0) return configured;

        var missing = string.IsNullOrWhiteSpace(target) ? "any database" : $"the target database '{target}'";
        throw new InvalidOperationException(
            $"No connection string is configured for {missing}. Configure {ConnectionStringsSection}:<DatabaseName> " +
            "(e.g. GafferDB, GafferDB_Test) with " +
            "dotnet user-secrets set \"ConnectionStrings:GafferDB_Test\" \"Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=<user>;Pwd=<password>;\" --project Gaffer.Api.Migrations " +
            "or the environment variable ConnectionStrings__GafferDB_Test. Use --Target <DatabaseName> to migrate a single database.");
    }

    private static void ApplyMigrationsIn(string connectionString, string databaseName,
        IServiceCollection serviceCollection)
    {
        var serviceProvider = serviceCollection
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddMySql5()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(M0001_CreatePlayersTable).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        try
        {
            Console.WriteLine($"Creating Database {databaseName} id not exists");
            EnsureDatabaseExists($"{connectionString}", databaseName);

            Console.WriteLine($"Migrations for {databaseName} running...");
            runner.MigrateUp();

            Console.WriteLine();
            Console.WriteLine($"Migrations for {databaseName} finished.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Environment.Exit(1);
        }
    }

    static void EnsureDatabaseExists(string connectionString, string databaseName)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
        CREATE DATABASE IF NOT EXISTS {databaseName}
            CHARACTER SET utf8mb4
            COLLATE utf8mb4_unicode_ci;";
        command.ExecuteNonQuery();

        Console.WriteLine($"Database '{databaseName}' is ready.");
    }

}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;
    public string Name { get; set; } = null!;
}