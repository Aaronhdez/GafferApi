using System.Data.Common;
using AwesomeAssertions;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Startups;
using Gaffer.Api.Tests.TestShared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Gaffer.Api.Tests.Integration;

[NonParallelizable]
public class ApiDatabaseConfigurationShould
{
    [Test]
    public void NotKeepAPasswordInTheVersionedDevelopmentSettings()
    {
        var settings = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(ApiProjectDirectory(), "appsettings.Development.json"), optional: false)
            .Build();

        var connectionString = new DbConnectionStringBuilder
        {
            ConnectionString = settings["DatabaseConfig:ConnectionString"] ?? string.Empty
        };

        connectionString.ContainsKey("Pwd").Should().BeFalse();
        connectionString.ContainsKey("Password").Should().BeFalse();
    }

    [Test]
    public async Task ReceiveTheConnectionStringFromTheEnvironmentOverTheAppSettings()
    {
        var connectionString = TestConfiguration.DatabaseConnectionString;
        using var _ = new EnvironmentVariableScope("DatabaseConfig__ConnectionString", connectionString);
        using var host = Host.CreateDefaultBuilder()
            .UseEnvironment("Development")
            .UseContentRoot(ApiProjectDirectory())
            .ConfigureServices((context, services) => services.ConfigureDatabase(context.Configuration))
            .Build();

        host.Services.GetRequiredService<IOptions<DatabaseConfig>>().Value.ConnectionString
            .Should().Be(connectionString);
        await using var scope = host.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>()
            .Should().BeOfType<SqlConnectionProvider>();
    }

    private static string ApiProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "gaffer-api.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("gaffer-api.sln"), "Gaffer.Api");
    }
}
