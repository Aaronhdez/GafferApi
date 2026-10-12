using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;

namespace Gaffer.Api.Tests.Unit.Configuration;

[NonParallelizable]
public class TestConfigurationShould {
    private const string EnvironmentConnectionString = "Server=env-host;Database=GafferDB_Test;Uid=env-user;Pwd=env-secret;";

    [Test]
    public void PreferTheEnvironmentVariableOverTheTestSettingsFile() {
        using var _ = new EnvironmentVariableScope("ConnectionStrings__Database", EnvironmentConnectionString);

        var configuration = TestConfiguration.Build();

        configuration[TestConfiguration.DatabaseKey].Should().Be(EnvironmentConnectionString);
    }

    [Test]
    public void ReadTestSettingsThenUserSecretsThenEnvironmentVariables() {
        var providers = TestConfiguration.Build().Providers.ToList();

        providers.Should().HaveCount(3);
        providers[0].Should().BeOfType<JsonConfigurationProvider>()
            .Which.Source.Path.Should().Be("testSettings.json");
        providers[1].Should().BeOfType<JsonConfigurationProvider>()
            .Which.Source.Path.Should().Be("secrets.json");
        providers[2].Should().BeOfType<EnvironmentVariablesConfigurationProvider>();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;")]
    [TestCase("Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=;")]
    public void FailWithGuidanceWhenTheConnectionStringHasNoPassword(string? connectionString) {
        var configuration = InMemory(connectionString);

        var read = () => TestConfiguration.RequireDatabaseConnectionString(configuration);

        read.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(TestConfiguration.DatabaseKey)
            .And.Contain("dotnet user-secrets set")
            .And.Contain("--project Gaffer.Api.Tests")
            .And.Contain("ConnectionStrings__Database");
    }

    [TestCase("Server=127.0.0.1;Database=GafferDB_Test;Uid=root;Pwd=a-secret;")]
    [TestCase("Server=127.0.0.1;Database=GafferDB_Test;User Id=root;Password=a-secret;")]
    public void ReturnAConnectionStringThatHasAPassword(string connectionString) => TestConfiguration.RequireDatabaseConnectionString(InMemory(connectionString)).Should().Be(connectionString);

    [Test]
    public void DeriveAnUnreachableConnectionStringChangingOnlyThePortAndTimeout() {
        const string reachable = "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=a-secret;";

        var unreachable = new System.Data.Common.DbConnectionStringBuilder {
            ConnectionString = TestConfiguration.ToUnreachable(reachable)
        };

        unreachable["Port"].Should().Be("1");
        unreachable["Connection Timeout"].Should().Be("2");
        unreachable["Server"].Should().Be("127.0.0.1");
        unreachable["Database"].Should().Be("GafferDB_Test");
        unreachable["Uid"].Should().Be("root");
        unreachable["Pwd"].Should().Be("a-secret");
    }

    private static IConfiguration InMemory(string? connectionString) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { [TestConfiguration.DatabaseKey] = connectionString })
        .Build();
}
