using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance;

public class AnonymousAccessShould {
    [Test]
    public async Task ServeAnAnonymousRequestWithoutCookiesOrRedirects() {
        await using var server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);

        var response = await server.Client.GetAsync("/api/v1.0/facilities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        response.Headers.Location.Should().BeNull();
    }
}
