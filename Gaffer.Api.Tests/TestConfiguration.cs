using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace Gaffer.Api.Tests;

/// <summary>
/// Single source of the test configuration, including the MySQL connection string.
/// Precedence (last wins): testSettings.json, User Secrets of Gaffer.Api.Tests, environment variables.
/// </summary>
public static class TestConfiguration
{
    public const string DatabaseKey = "ConnectionStrings:Database";

    private static readonly Lazy<IConfigurationRoot> Root = new(Build);

    public static IConfigurationRoot Build() => AddSources(new ConfigurationBuilder()).Build();

    public static IConfigurationBuilder AddSources(IConfigurationBuilder builder) =>
        builder.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("testSettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets(typeof(TestConfiguration).Assembly, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

    public static string DatabaseConnectionString => RequireDatabaseConnectionString(Root.Value);

    public static string UnreachableDatabaseConnectionString => ToUnreachable(DatabaseConnectionString);

    /// <summary>
    /// Returns the MySQL test connection string, or fails (never skips) explaining how to configure it.
    /// The configured value is never included in the message.
    /// </summary>
    public static string RequireDatabaseConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration[DatabaseKey];
        if (!HasPassword(connectionString))
        {
            throw new InvalidOperationException(
                $"The MySQL test connection string '{DatabaseKey}' has no password. MySQL-backed tests are not skipped: " +
                "configure it once per machine with " +
                $"dotnet user-secrets set \"{DatabaseKey}\" \"Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=<user>;Pwd=<password>;\" --project Gaffer.Api.Tests " +
                "or override it with the environment variable ConnectionStrings__Database.");
        }

        return connectionString!;
    }

    /// <summary>Same connection string pointing at a closed port with a short timeout.</summary>
    public static string ToUnreachable(string connectionString) =>
        new DbConnectionStringBuilder { ConnectionString = connectionString, ["Port"] = "1", ["Connection Timeout"] = "2" }
            .ConnectionString;

    private static bool HasPassword(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return new[] { "Pwd", "Password" }
            .Any(key => builder.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value?.ToString()));
    }
}
