using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Integration;

[TestFixture]
public class ApiShould {
    private IGafferApiClient client;

    [SetUp]
    public void Setup() => client = TestFixture.GafferApiClientTest;

    [Test]
    public async Task StatusIsUp() {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:50000/swagger/index.html");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}
