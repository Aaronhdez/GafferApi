using System.Collections;
using Azure.Identity;
using FluentMigrator;
using FluentMigrator.Runner;
using Gaffer.Api.Migrations.Migrations;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Gaffer.Api.Migrations;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Gaffer Api Migrations");
        List<DatabaseSettings> databaseSettings = new List<DatabaseSettings>(){
            new()
            {
                ConnectionString = "Server=127.0.0.1;Port=3306;Database=GafferDB;Uid=root;Pwd=toor;",
                Name = "GafferDB"
            },
            new()
            {
                ConnectionString = "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=toor;",
                Name = "GafferDB_Test"
            }
        };
        foreach (var databaseSetting in databaseSettings)
        {
            var serviceCollection = new ServiceCollection();
            ApplyMigrationsIn(databaseSetting.ConnectionString, databaseSetting.Name, serviceCollection);
            serviceCollection.Clear();
        }
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

    static void EnsureDatabaseExists(string connectionString, string databaseName = null)
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
    public string ConnectionString { get; set; }
    public string Name { get; set; }
}