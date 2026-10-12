using System.Data.Common;
using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance;

/// <summary>
/// Health endpoints served by the production pipeline (<see cref="StartupTestServer"/>, OPT-022): one host per
/// situation of the contract. The probe matrix (timeout, cancellation) lives in the unit tests of the check.
/// </summary>
public class HealthCheckShould {
    private const string Live = "/health/live";
    private const string Ready = "/health/ready";

    [Test]
    public async Task ReportLiveAndReadyAsHealthyWhenTheDatabaseIsReachable() {
        await using var server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);

        await ShouldAnswer(server, Live, HttpStatusCode.OK, "Healthy");
        await ShouldAnswer(server, Ready, HttpStatusCode.OK, "Healthy");
    }

    [Test]
    public async Task ReportReadinessAsUnhealthyWhenTheApiFellBackToTheDummyProviderWithoutExposingTheConnection() {
        var unreachable = TestConfiguration.UnreachableDatabaseConnectionString;
        await using var server = await StartupTestServer.StartAsync(enabled: true, unreachable);
        await using (var scope = server.Services.CreateAsyncScope()) {
            var provider = scope.ServiceProvider.GetRequiredService<IDatabaseConnectionProvider>();
            provider.Should().BeOfType<DummyConnectionProvider>();
            (await provider.TestConnection()).Should().BeTrue("the dummy provider always says it is connected");
        }

        await ShouldAnswer(server, Live, HttpStatusCode.OK, "Healthy");
        var body = await ShouldAnswer(server, Ready, HttpStatusCode.ServiceUnavailable, "Unhealthy");
        ShouldNotExposeTheConnection(body, unreachable);
    }

    [TestCase(false, true, TestName = "ReportReadinessAsDegradedWhenTheDatabaseIsDisabled")]
    [TestCase(true, false, TestName = "ReportReadinessAsDegradedWhenThereIsNoConnectionString")]
    public async Task ReportReadinessAsDegradedWhenTheDummyProviderIsIntended(bool enabled, bool configured) {
        await using var server = await StartupTestServer.StartAsync(
            enabled, configured ? TestDefaultValues.TestConnectionString : null);

        await ShouldAnswer(server, Live, HttpStatusCode.OK, "Healthy");
        await ShouldAnswer(server, Ready, HttpStatusCode.OK, "Degraded");
    }

    private static async Task<string> ShouldAnswer(StartupTestServer server, string path, HttpStatusCode status, string body) {
        var response = await server.Client.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();

        TestContext.Out.WriteLine($"GET {path}: {(int)response.StatusCode} {content}");
        response.StatusCode.Should().Be(status, content);
        content.Should().Be(body);
        return content;
    }

    private static void ShouldNotExposeTheConnection(string body, string connectionString) {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        foreach (var key in new[] { "Server", "Uid", "User", "User Id", "Pwd", "Password", "Database" }) {
            if (builder.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value?.ToString())) {
                body.Should().NotContain(value!.ToString());
            }
        }
        body.Should().NotContain("Exception").And.NotContain("MySql");
    }
}
