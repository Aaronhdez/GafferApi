using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Queries;
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

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
    }

    [Test]
    public async Task ReturnTheLocationOfTheCreatedFacility()
    {
        var response = await SendCreateFacilityRequest(BuildCityArenaRequestBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1.0/facilities/{createdId}");
        var getResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, response.Headers.Location));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var facility = await getResponse.Content.ReadFromJsonAsync<SearchFacilityResult>(TestJson.Options);
        facility!.Id.Should().Be(createdId);
        facility.Name.Should().Be("City Arena");
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

    [Test]
    public async Task GetAFacilityWithAddressAndStands()
    {
        var createResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await SendGetFacilityRequest(createdId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var facility = await response.Content.ReadFromJsonAsync<SearchFacilityResult>(TestJson.Options);
        facility.Should().NotBeNull();
        facility!.Id.Should().Be(createdId);
        facility.Name.Should().Be("City Arena");
        facility.Address.City.Should().Be("Barcelona");
        facility.Stands.Should().HaveCount(4);
    }

    [Test]
    public async Task ReturnNotFoundWhenGettingAnUnknownFacility()
    {
        var response = await SendGetFacilityRequest(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAllFacilitiesIncludingRecentlyCreatedOnes()
    {
        var firstCreateResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var firstId = await firstCreateResponse.Content.ReadFromJsonAsync<Guid>();
        var secondCreateResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var secondId = await secondCreateResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Uris.GetAllFacilities()));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var facilities = await response.Content.ReadFromJsonAsync<List<SearchFacilityResult>>(TestJson.Options);
        facilities.Should().NotBeNull();
        facilities!.Should().Contain(f => f.Id == firstId);
        facilities.Should().Contain(f => f.Id == secondId);
    }

    [Test]
    public async Task DeleteAFacilityAndMakeItUnreachableAfterwards()
    {
        var createResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Uris.DeleteFacility(createdId.ToString())));

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await SendGetFacilityRequest(createdId);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ReturnNotFoundWhenDeletingAnUnknownFacility()
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Uris.DeleteFacility(Guid.NewGuid().ToString())));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateAFacilityReplacingFieldsAddressAndStands()
    {
        var createResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var updateRequestBody = new
        {
            Name = "Renovated Arena",
            Active = false,
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
            },
            Stands = new[]
            {
                new { StandName = "Only", StandCapacity = 60000 }
            }
        };

        var updateResponse = await SendUpdateFacilityRequest(createdId, updateRequestBody);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await SendGetFacilityRequest(createdId);
        var facility = await getResponse.Content.ReadFromJsonAsync<SearchFacilityResult>(TestJson.Options);
        facility.Should().NotBeNull();
        facility!.Name.Should().Be("Renovated Arena");
        facility.Active.Should().BeFalse();
        facility.Capacity.Should().Be(60000);
        facility.Address.Street.Should().Be("New Boulevard");
        facility.Address.City.Should().Be("Madrid");
        facility.Stands.Should().ContainSingle(stand => stand.StandName == "Only" && stand.StandCapacity == 60000);
    }

    [Test]
    public async Task ReturnNotFoundWhenUpdatingAnUnknownFacility()
    {
        var updateRequestBody = new
        {
            Name = "Renovated Arena",
            Active = false,
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
        };

        var response = await SendUpdateFacilityRequest(Guid.NewGuid(), updateRequestBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RejectUpdatingAFacilityWithMoreThanFourStands()
    {
        var createResponse = await SendCreateFacilityRequest(BuildCityArenaRequestBody());
        var createdId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var updateRequestBody = new
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

        var response = await SendUpdateFacilityRequest(createdId, updateRequestBody);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    private static object BuildCityArenaRequestBody() => new
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

    private async Task<HttpResponseMessage> SendCreateFacilityRequest(object requestBody)
    {
        var json = JsonConvert.SerializeObject(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, Uris.CreateFacility())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendGetFacilityRequest(Guid id)
    {
        return await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Uris.GetFacility(id.ToString())));
    }

    private async Task<HttpResponseMessage> SendUpdateFacilityRequest(Guid id, object requestBody)
    {
        var json = JsonConvert.SerializeObject(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Put, Uris.UpdateFacility(id.ToString()))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }
}