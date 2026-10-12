using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api.Tests.Integration;

/// <summary>
/// Builds (without running) the same host that <see cref="Program.Main"/> runs and checks which
/// environment it takes from <c>ASPNETCORE_ENVIRONMENT</c>. The variable is process-wide, so the
/// fixture is not parallelizable and every test restores it.
/// </summary>
[NonParallelizable]
public class HostingEnvironmentShould {
    private const string EnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

    [TestCase("Staging", "Staging")]
    [TestCase("Production", "Production")]
    [TestCase(null, "Development")]
    [TestCase("", "Development")]
    [TestCase("   ", "Development")]
    public void TakeTheHostEnvironmentFromTheEnvironmentVariable(string? variableValue, string expectedEnvironment) {
        using var _ = new EnvironmentVariableScope(EnvironmentVariable, variableValue);
        using var __ = new EnvironmentVariableScope("DatabaseConfig__Enabled", "false");

        using var host = Program.CreateGafferHostBuilder([]).Build();

        host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.Should().Be(expectedEnvironment);
        Program.CreateProductContext().Environment.Should().Be(expectedEnvironment);
    }

    /// <summary>
    /// The default host loads <c>appsettings.json</c> and <c>appsettings.{Environment}.json</c> from the content root.
    /// <c>appsettings.json</c> is empty (<c>{}</c>), so the value read comes from <c>appsettings.Development.json</c>.
    /// </summary>
    [Test]
    public void ReadTheAppSettingsFilesFromTheContentRoot() {
        using var _ = new EnvironmentVariableScope(EnvironmentVariable, "Development");
        using var __ = new EnvironmentVariableScope("DatabaseConfig__Enabled", "false");

        using var host = Program.CreateGafferHostBuilder([]).Build();

        var configuration = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        configuration.Providers.OfType<JsonConfigurationProvider>().Select(provider => provider.Source.Path)
            .Should().ContainInOrder("appsettings.json", "appsettings.Development.json");
        configuration["InfrastructureOptions:EnableCors"].Should().Be("False");
    }

    [Test]
    public void RestoreTheEnvironmentVariableAfterEachTest() {
        var before = Environment.GetEnvironmentVariable(EnvironmentVariable);

        using (new EnvironmentVariableScope(EnvironmentVariable, "Staging")) {
            Environment.GetEnvironmentVariable(EnvironmentVariable).Should().Be("Staging");
        }

        Environment.GetEnvironmentVariable(EnvironmentVariable).Should().Be(before);
    }
}
