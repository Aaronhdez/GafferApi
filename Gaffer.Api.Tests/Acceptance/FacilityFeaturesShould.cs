using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared.Clients;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public async Task UpdateAFacilityFieldsAndAddressKeepingItsFourStands()
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
        facility.Stands.Select(stand => stand.StandName).Should().BeEquivalentTo(["North", "South", "East", "West"]);
        facility.Stands.Should().NotContain(stand => stand.StandName == "Only");
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task KeepTheIdsOfTheStandsSentWithTheirIdWhileUpdatingTheirValues()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(facilityId);
        var north = before.Stands.Single(stand => stand.StandName == "North");
        var south = before.Stands.Single(stand => stand.StandName == "South");

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithId(north.Id, "North Renovated", 5000),
            StandWithId(south.Id, "South Renovated", 6000)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetFacility(facilityId);
        LogStandIds(before, after);
        after.Stands.Should().BeEquivalentTo([
            new SearchFacilityStandResult(north.Id, "North Renovated", 5000),
            new SearchFacilityStandResult(south.Id, "South Renovated", 6000)
        ]);
        after.Address.Id.Should().Be(before.Address.Id);
        after.Address.Street.Should().Be("New Boulevard");
    }

    [Test]
    public async Task KeepTheStandsNotSentAndCreateTheOnesSentWithoutId()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(facilityId);
        var north = before.Stands.Single(stand => stand.StandName == "North");
        var south = before.Stands.Single(stand => stand.StandName == "South");

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithId(north.Id, "North Renovated", 5000),
            StandWithoutId("East", 3000)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetFacility(facilityId);
        LogStandIds(before, after);
        after.Stands.Should().HaveCount(3);
        after.Stands.Should().ContainEquivalentOf(new SearchFacilityStandResult(north.Id, "North Renovated", 5000));
        after.Stands.Should().ContainEquivalentOf(south);
        var east = after.Stands.Single(stand => stand.StandName == "East");
        east.StandCapacity.Should().Be(3000);
        east.Id.Should().NotBe(north.Id).And.NotBe(south.Id);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task KeepTheStandsWhenTheUpdateSendsNoStands(bool sendAnEmptyList)
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(facilityId);

        var response = await SendUpdateFacilityRequest(facilityId,
            BuildUpdateRequestBodyWithName("Renovated Arena", sendAnEmptyList ? Array.Empty<object>() : null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetFacility(facilityId);
        after.Name.Should().Be("Renovated Arena");
        after.Stands.Should().BeEquivalentTo(before.Stands);
    }

    [Test]
    public async Task CreateStandsWithNewIdsForUnknownOrForeignIdsWhenTheFacilityHasLessThanFourStands()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var otherFacilityId = await CreateFacilityWithStands("Other");
        var before = await GetFacility(facilityId);
        var otherBefore = await GetFacility(otherFacilityId);
        var unknownId = Guid.NewGuid();
        var foreignId = otherBefore.Stands.Single().Id;

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithId(unknownId, "Unknown", 1000),
            StandWithId(foreignId, "Foreign", 2000)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetFacility(facilityId);
        LogStandIds(before, after);
        TestContext.Out.WriteLine($"sent ids: unknown={unknownId} foreign={foreignId}");
        after.Stands.Should().HaveCount(4);
        foreach (var stand in before.Stands)
        {
            after.Stands.Should().ContainEquivalentOf(stand);
        }

        var created = after.Stands.Where(stand => before.Stands.All(existing => existing.Id != stand.Id)).ToList();
        created.Select(stand => $"{stand.StandName}:{stand.StandCapacity}")
            .Should().BeEquivalentTo(["Unknown:1000", "Foreign:2000"]);
        created.Select(stand => stand.Id).Should().NotContain(unknownId).And.NotContain(foreignId);
        (await GetFacility(otherFacilityId)).Should().BeEquivalentTo(otherBefore);
    }

    [Test]
    public async Task RejectAnUpdateLeavingMoreThanFourStandsWhenTwoExistAndThreeAreSentWithoutId()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(facilityId);

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithoutId("A", 1000),
            StandWithoutId("B", 1000),
            StandWithoutId("C", 1000)));

        await AssertRejectedWithoutChanges(response, "Error.Facilities.Create.Validation", facilityId, before);
    }

    [Test]
    public async Task RejectAnUpdateLeavingMoreThanFourStandsWhenThreeExistAndOneIsNewAndOneUnknown()
    {
        var facilityId = await CreateFacilityWithStands("North", "South", "East");
        var before = await GetFacility(facilityId);

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithoutId("A", 1000),
            StandWithId(Guid.NewGuid(), "B", 1000)));

        await AssertRejectedWithoutChanges(response, "Error.Facilities.Create.Validation", facilityId, before);
    }

    [Test]
    public async Task DiscardSilentlyEveryUnmatchedStandWhenTheFacilityAlreadyHasFourStands()
    {
        var facilityId = await CreateFacilityWithStands("North", "South", "East", "West");
        var otherFacilityId = await CreateFacilityWithStands("Other");
        var before = await GetFacility(facilityId);
        var otherBefore = await GetFacility(otherFacilityId);
        var north = before.Stands.Single(stand => stand.StandName == "North");
        var newId = Guid.NewGuid();
        var foreignId = otherBefore.Stands.Single().Id;

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithId(north.Id, "North Renovated", 5000),
            StandWithId(newId, "Discarded New", 1000),
            StandWithoutId("Discarded Without Id", 1000),
            StandWithId(foreignId, "Discarded Foreign", 1000)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        var after = await GetFacility(facilityId);
        LogStandIds(before, after);
        TestContext.Out.WriteLine($"sent ids: new={newId} foreign={foreignId}");
        after.Name.Should().Be("Renovated Arena");
        after.Stands.Select(stand => stand.Id).Should().BeEquivalentTo(before.Stands.Select(stand => stand.Id));
        after.Stands.Should().ContainEquivalentOf(new SearchFacilityStandResult(north.Id, "North Renovated", 5000));
        foreach (var stand in before.Stands.Where(stand => stand.Id != north.Id))
        {
            after.Stands.Should().ContainEquivalentOf(stand);
        }

        after.Stands.Should().NotContain(stand => stand.Id == newId || stand.StandName.StartsWith("Discarded"));
        (await GetFacility(otherFacilityId)).Should().BeEquivalentTo(otherBefore);
    }

    [TestCase(2)]
    [TestCase(4)]
    public async Task RejectAnUpdateRepeatingTheIdOfAnExistingStand(int existingStands)
    {
        var facilityId = await CreateFacilityWithStands(new[] { "North", "South", "East", "West" }.Take(existingStands).ToArray());
        var before = await GetFacility(facilityId);
        var north = before.Stands.Single(stand => stand.StandName == "North");

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithId(north.Id, "First", 1000),
            StandWithId(north.Id, "Second", 2000)));

        await AssertRejectedWithoutChanges(response, "Error.Facilities.Update.DuplicatedStand", facilityId, before);
    }

    private async Task<Guid> CreateFacilityWithStands(params string[] standNames)
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
            Stands = standNames.Select(name => new { StandName = name, StandCapacity = 4500 }).ToArray()
        };
        var response = await SendCreateFacilityRequest(requestBody);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<SearchFacilityResult> GetFacility(Guid id)
    {
        var response = await SendGetFacilityRequest(id);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var facility = await response.Content.ReadFromJsonAsync<SearchFacilityResult>(TestJson.Options);
        facility.Should().NotBeNull();
        return facility!;
    }

    private async Task AssertRejectedWithoutChanges(
        HttpResponseMessage response, string translationKey, Guid facilityId, SearchFacilityResult before)
    {
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);
        var body = JObject.Parse(raw);
        body["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be(translationKey);
        (await GetFacility(facilityId)).Should().BeEquivalentTo(before);
    }

    private static void LogStandIds(SearchFacilityResult before, SearchFacilityResult after)
    {
        TestContext.Out.WriteLine($"before: {string.Join(", ", before.Stands.Select(stand => $"{stand.StandName}={stand.Id}"))}");
        TestContext.Out.WriteLine($"after: {string.Join(", ", after.Stands.Select(stand => $"{stand.StandName}={stand.Id}"))}");
    }

    private static object StandWithId(Guid id, string standName, int standCapacity) =>
        new { Id = id, StandName = standName, StandCapacity = standCapacity };

    private static object StandWithoutId(string standName, int standCapacity) =>
        new { StandName = standName, StandCapacity = standCapacity };

    private static object BuildUpdateRequestBody(params object[] stands) =>
        BuildUpdateRequestBodyWithName("Renovated Arena", stands);

    private static object BuildUpdateRequestBodyWithName(string name, object[]? stands) => new
    {
        Name = name,
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
        Stands = stands
    };

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