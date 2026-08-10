namespace Gaffer.Api.Tests.Integration;

[TestFixture]
public class ApiShould
{
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _client = TestFixture.Client;
    }

    [Test]
    public async Task StatusIsUp()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/swagger/index.html");
        
        var response = await _client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }
}