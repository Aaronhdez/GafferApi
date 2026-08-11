using Gaffer.Api.Shared.Clients;

namespace Gaffer.Api.Tests.Integration;

[TestFixture]
public class ApiShould
{
    private IGafferApiClient _client;

    [SetUp]
    public void Setup()
    {
        _client = TestFixture.GafferApiClientTest;
    }

    [Test]
    public async Task StatusIsUp()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:50000/swagger/index.html");
        
        var response = await _client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }
}