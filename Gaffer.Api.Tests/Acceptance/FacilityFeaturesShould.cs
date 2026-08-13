using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Shared.Clients;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;

namespace Gaffer.Api.Tests.Acceptance;

public class FacilityFeaturesShould
{
    private IGafferApiClient _client;

    [SetUp]
    public void SetUp()
    {
        _client = TestFixture.GafferApiClientTest;
    }

    [Test]
    public async Task CreateAFacilityWithAddressAndNoStands()
    {
        var requestBody = new
        {
            Name = "National Stadium",
            Active = true,
            BuildDate = new DateTime(2018, 1, 5),
            Type = "Stadium",
            Capacity = 52000,
            Address = new
            {
                Street = "Main Avenue",
                StreetNumber = "100",
                PostalCode = "28001",
                City = "Madrid",
                Region = "Madrid",
                Country = "Spain"
            }
        };

        var response = await SendCreateFacilityRequest(requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
    }

    [Test]
    public async Task CreateAFacilityWithAddressAndUpToFourStands()
    {
        var requestBody = new
        {
            Name = "City Arena",
            Active = true,
            BuildDate = new DateTime(2020, 7, 20),
            Type = "Arena",
            Capacity = 18000,
            Address = new
            {
                Street = "Arena Road",
                StreetNumber = "50",
                PostalCode = "08001",
                City = "Barcelona",
                Region = "Catalonia",
                Country = "Spain"
            },
            Stands = new[]
            {
                new { StandName = "North", StandCapacity = 4500 },
                new { StandName = "South", StandCapacity = 4500 },
                new { StandName = "East", StandCapacity = 4500 },
                new { StandName = "West", StandCapacity = 4500 }
            }
        };

        var response = await SendCreateFacilityRequest(requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
    }

    [Test]
    public async Task RejectCreatingAFacilityWithMissingAddress()
    {
        var requestBody = new
        {
            Name = "Training Center",
            Active = true,
            BuildDate = new DateTime(2021, 3, 15),
            Type = "Training",
            Capacity = 1200,
            Stands = new[]
            {
                new { StandName = "Main", StandCapacity = 300 }
            }
        };

        var response = await SendCreateFacilityRequest(requestBody);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task RejectCreatingAFacilityWithMoreThanFourStands()
    {
        var requestBody = new
        {
            Name = "Regional Pitch",
            Active = true,
            BuildDate = new DateTime(2022, 11, 30),
            Type = "Pitch",
            Capacity = 6000,
            Address = new
            {
                Street = "Pitch Street",
                StreetNumber = "8",
                PostalCode = "46001",
                City = "Valencia",
                Region = "Valencian Community",
                Country = "Spain"
            },
            Stands = new[]
            {
                new { StandName = "A", StandCapacity = 1000 },
                new { StandName = "B", StandCapacity = 1000 },
                new { StandName = "C", StandCapacity = 1000 },
                new { StandName = "D", StandCapacity = 1000 },
                new { StandName = "E", StandCapacity = 1000 }
            }
        };

        var response = await SendCreateFacilityRequest(requestBody);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    private async Task<HttpResponseMessage> SendCreateFacilityRequest(object requestBody)
    {
        var json = JsonConvert.SerializeObject(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, Uris.CreateFacility())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }
}