using AwesomeAssertions;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Startups;
using Gaffer.Api.Tests.TestShared;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gaffer.Api.Tests.Integration;

public class DatabaseStartupShould
{
    private const string PlayersInsert =
        "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
        "VALUES (@Id, 'Isolation', 'Probe', NULL, 'MALE', '2000-01-01');";
    private const string PlayerCount = "SELECT COUNT(*) FROM `Players` WHERE Id = @Id;";

    [SetUp]
    [TearDown]
    public async Task ClearPlayers()
        => await new SqlConnectionProvider(TestDefaultValues.TestConnectionString)
            .ClearAsync([DbTableNames.PlayersTable]);

    [Test]
    public async Task ResolveADifferentConnectionProviderPerScopeAndTheSameOneWithinAScope()
    {
        await using var serviceProvider = BuildServiceProvider(enabled: true, TestDefaultValues.TestConnectionString);
        await using var scopeA = serviceProvider.CreateAsyncScope();
        await using var scopeB = serviceProvider.CreateAsyncScope();

        var fromA = scopeA.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var fromAAgain = scopeA.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var fromB = scopeB.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();

        fromA.Should().BeOfType<SqlConnectionProvider>();
        fromA.Should().BeSameAs(fromAAgain);
        fromA.Should().NotBeSameAs(fromB);
    }

    [Test]
    public async Task IsolateAnUncommittedTransactionOfOneScopeFromAnotherScope()
    {
        await using var serviceProvider = BuildServiceProvider(enabled: true, TestDefaultValues.TestConnectionString);
        await using var scopeA = serviceProvider.CreateAsyncScope();
        await using var scopeB = serviceProvider.CreateAsyncScope();
        var providerA = scopeA.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var providerB = scopeB.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var id = Guid.NewGuid();
        long countSeenFromB = -1;

        var transactionInA = async () => await providerA.ExecuteInTransactionAsync(async () =>
        {
            await providerA.ExecuteAsync(PlayersInsert, new { Id = id });
            countSeenFromB = await providerB.QueryScalarAsync<long>(PlayerCount, new { Id = id });
            throw new InvalidOperationException("force rollback");
        });

        await transactionInA.Should().ThrowAsync<InvalidOperationException>();
        countSeenFromB.Should().Be(0);
        var countAfterRollback = await providerB.QueryScalarAsync<long>(PlayerCount, new { Id = id });
        countAfterRollback.Should().Be(0);
    }

    [Test]
    public async Task ResolveADifferentConnectionProviderPerScopeInTheTestServer()
    {
        await using var scopeA = TestFixture.TestServer.Services.CreateAsyncScope();
        await using var scopeB = TestFixture.TestServer.Services.CreateAsyncScope();

        var fromA = scopeA.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
        var fromB = scopeB.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();

        fromA.Should().NotBeSameAs(fromB);
        fromA.Should().NotBeSameAs(TestFixture.SqlConnectionProviderTest);
    }

    [Test]
    public async Task FallBackToTheDummyProviderWithoutThrowingWhenTheDatabaseIsUnreachable()
    {
        var configure = () => BuildServiceProvider(enabled: true, UnreachableConnectionString);

        await using var serviceProvider = configure.Should().NotThrow().Subject;
        await using var scope = serviceProvider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>()
            .Should().BeOfType<DummyConnectionProvider>();
    }

    [Test]
    public void LogOnceThatTheDummyProviderIsUsedWhenTheDatabaseIsUnreachable()
    {
        var logger = new CapturingLogger();

        using var serviceProvider = BuildServiceProvider(enabled: true, UnreachableConnectionString, logger);

        var entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("not reachable").And.Contain(nameof(DummyConnectionProvider));
        entry.Message.Should().NotContain("Pwd=");
    }

    [Test]
    public void NotLogTheFallbackWhenTheDatabaseIsReachable()
    {
        var logger = new CapturingLogger();

        using var serviceProvider = BuildServiceProvider(enabled: true, ReachableConnectionString, logger);

        logger.Entries.Should().BeEmpty();
    }

    [Test]
    public async Task GiveTheRegisteredHealthCheckTheHostLoggerSoAFailedProbeIsLogged()
    {
        var logs = new RecordingLoggerProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseConfig:Enabled"] = "true",
                ["DatabaseConfig:ConnectionString"] = ReachableConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        services.ConfigureDatabase(configuration, logger: null);
        // The provider is chosen with the real database; the probe used by the health check then fails.
        services.AddSingleton<IDatabaseProbe>(new UnreachableProbe());
        await using var serviceProvider = services.BuildServiceProvider();

        var report = await serviceProvider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Unhealthy);
        logs.EntriesFor<DatabaseHealthCheck>().Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Warning);
    }

    private sealed class UnreachableProbe : IDatabaseProbe
    {
        public Task<bool> IsReachableAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task KeepRegisteringTheDummyProviderWhenTheDatabaseIsDisabledOrNotConfigured(bool enabled, bool configured)
    {
        await using var serviceProvider = BuildServiceProvider(enabled, configured ? ReachableConnectionString : "");
        await using var scope = serviceProvider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>()
            .Should().BeOfType<DummyConnectionProvider>();
    }

    [Test]
    public async Task RegisterThatTheSqlProviderWasChosenWhenTheDatabaseIsReachable()
    {
        await using var serviceProvider = BuildServiceProvider(enabled: true, ReachableConnectionString);

        serviceProvider.GetRequiredService<DatabaseProviderSelection>().Reason.Should().Be(DatabaseProviderReason.Sql);
    }

    [Test]
    public async Task RegisterThatTheDummyProviderIsAFallbackWhenTheDatabaseIsUnreachable()
    {
        await using var serviceProvider = BuildServiceProvider(enabled: true, UnreachableConnectionString);

        serviceProvider.GetRequiredService<DatabaseProviderSelection>().Reason.Should().Be(DatabaseProviderReason.DummyByFallback);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task RegisterThatTheDummyProviderComesFromTheConfigurationWhenTheDatabaseIsDisabledOrNotConfigured(
        bool enabled, bool configured)
    {
        await using var serviceProvider = BuildServiceProvider(enabled, configured ? ReachableConnectionString : "");

        serviceProvider.GetRequiredService<DatabaseProviderSelection>().Reason.Should().Be(DatabaseProviderReason.DummyByConfiguration);
    }

    private static string ReachableConnectionString => TestConfiguration.DatabaseConnectionString;
    private static string UnreachableConnectionString => TestConfiguration.UnreachableDatabaseConnectionString;

    private static ServiceProvider BuildServiceProvider(bool enabled, string? connectionString, ILogger? logger = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseConfig:Enabled"] = enabled.ToString(),
                ["DatabaseConfig:ConnectionString"] = connectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.ConfigureDatabase(configuration, logger);
        return services.BuildServiceProvider();
    }
}

internal sealed class CapturingLogger : ILogger
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}
