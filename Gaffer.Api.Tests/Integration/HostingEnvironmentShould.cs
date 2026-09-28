using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Microsoft.Extensions.Hosting;

namespace Gaffer.Api.Tests.Integration;

/// <summary>
/// Builds (without running) the same host that <see cref="Program.Main"/> runs and checks which
/// environment it takes from <c>ASPNETCORE_ENVIRONMENT</c>. The variable is process-wide, so the
/// fixture is not parallelizable and every test restores it.
/// </summary>
[NonParallelizable]
public class HostingEnvironmentShould
{
    private const string EnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

    [TestCase("Staging", "Staging")]
    [TestCase("Production", "Production")]
    [TestCase(null, "Development")]
    [TestCase("", "Development")]
    [TestCase("   ", "Development")]
    public void TakeTheHostEnvironmentFromTheEnvironmentVariable(string? variableValue, string expectedEnvironment)
    {
        using var _ = new EnvironmentVariableScope(EnvironmentVariable, variableValue);
        using var __ = new EnvironmentVariableScope("DatabaseConfig__Enabled", "false");

        using var host = Program.CreateGafferHostBuilder([]).Build();

        host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName.Should().Be(expectedEnvironment);
        Program.CreateProductContext().Environment.Should().Be(expectedEnvironment);
    }

    [Test]
    public void RestoreTheEnvironmentVariableAfterEachTest()
    {
        var before = Environment.GetEnvironmentVariable(EnvironmentVariable);

        using (new EnvironmentVariableScope(EnvironmentVariable, "Staging"))
        {
            Environment.GetEnvironmentVariable(EnvironmentVariable).Should().Be("Staging");
        }

        Environment.GetEnvironmentVariable(EnvironmentVariable).Should().Be(before);
    }
}
