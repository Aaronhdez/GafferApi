using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;

namespace Gaffer.Api.Tests.Acceptance;

/// <summary>
/// Infrastructure failures (unreachable MySQL) during Delete/Update are not business errors:
/// they must end in 500, not in a 400 produced by wrapping them in generic exceptions.
/// Uses its own TestServer (same DI as <see cref="TestFixture"/>) pointing at an unreachable database.
/// </summary>
public class DatabaseFailureShould
{
    private TestServer _server;
    private HttpClient _client;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var baseAddress = TestFixture.Configuration["ApiUrl"];
        _server = await TestFixture.StartApiServer(baseAddress, TestConfiguration.UnreachableDatabaseConnectionString);
        _client = _server.CreateClient();
        _client.BaseAddress = new Uri(baseAddress!);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    [Test]
    public async Task ReturnInternalServerErrorWhenGettingAPlayer()
    {
        var response = await _client.GetAsync(Uris.GetPlayer(Guid.NewGuid().ToString()));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ReturnInternalServerErrorWhenDeletingAPlayer()
    {
        var response = await _client.DeleteAsync(Uris.DeletePlayer(Guid.NewGuid().ToString()));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task ReturnInternalServerErrorWhenUpdatingAPlayer()
    {
        var response = await _client.PutAsync(Uris.UpdatePlayer(Guid.NewGuid().ToString()),
            new StringContent("""{"firstName":"X"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task ReturnInternalServerErrorWhenDeletingAFacility()
    {
        var response = await _client.DeleteAsync(Uris.DeleteFacility(Guid.NewGuid().ToString()));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task ReturnInternalServerErrorWhenUpdatingAFacility()
    {
        var body = JsonConvert.SerializeObject(new
        {
            Name = "Renovated Arena",
            Active = true,
            BuildDate = new DateTime(2023, 5, 10),
            Type = "Stadium",
            Capacity = 60000,
            Address = new
            {
                Street = "New Boulevard",
                StreetNumber = "1",
                PostalCode = "28002",
                City = "Madrid",
                Region = "Madrid",
                Country = "Spain"
            }
        });

        var response = await _client.PutAsync(Uris.UpdateFacility(Guid.NewGuid().ToString()),
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await response.Content.ReadAsStringAsync());
    }
}
