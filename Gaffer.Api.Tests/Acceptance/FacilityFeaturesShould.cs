using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Queries;
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

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.CreateValidationKey, "Address");
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

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.CreateValidationKey, "Stands");
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

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, "Stands");
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

        await AssertRejectedWithoutChanges(response, "Error.Facilities.Update.Validation", facilityId, before);
    }

    [Test]
    public async Task RejectAnUpdateLeavingMoreThanFourStandsWhenThreeExistAndOneIsNewAndOneUnknown()
    {
        var facilityId = await CreateFacilityWithStands("North", "South", "East");
        var before = await GetFacility(facilityId);

        var response = await SendUpdateFacilityRequest(facilityId, BuildUpdateRequestBody(
            StandWithoutId("A", 1000),
            StandWithId(Guid.NewGuid(), "B", 1000)));

        await AssertRejectedWithoutChanges(response, "Error.Facilities.Update.Validation", facilityId, before);
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

    // --- Step 03a: domain validation of the facility and its address (create/update) ---

    [Test]
    public async Task CreateAFacilityWhoseValuesAreAtTheLimits()
    {
        foreach (var (name, change) in FacilityValidationCases.Boundaries())
        {
            var body = FacilityValidationCases.ValidBody(change);

            var response = await PostRawJson(body);

            var raw = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.Created, $"{name}: {raw}");
            var created = await GetFacility(JsonConvert.DeserializeObject<Guid>(raw));
            FacilityValidationCases.ShouldMatchTheSentBody(created, body, name);
        }
    }

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.InvalidBodies))]
    public async Task RejectTheCreationOfAnInvalidFacilityWithoutSavingIt(string field, Action<JObject> change)
    {
        var before = await FacilityValidationCases.CountStoredRows();

        var response = await PostRawJson(FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.CreateValidationKey, field);
        (await FacilityValidationCases.CountStoredRows()).Should().Be(before);
    }

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.InvalidBodies))]
    public async Task RejectTheUpdateOfAFacilityWithAnInvalidBodyAndChangeNothing(string field, Action<JObject> change)
    {
        var createdId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(createdId);

        var response = await PutRawJson(createdId, FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, field);
        (await GetFacility(createdId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task UpdateAFacilityWithValuesAtTheLimits()
    {
        var createdId = await CreateFacilityWithStands("North");
        foreach (var (name, change) in FacilityValidationCases.Boundaries())
        {
            var body = FacilityValidationCases.ValidBody(change);

            var response = await PutRawJson(createdId, body);

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{name}: {await response.Content.ReadAsStringAsync()}");
            FacilityValidationCases.ShouldMatchTheSentBody(await GetFacility(createdId), body, name);
        }
    }

    [Test]
    public async Task ReturnNotFoundWhenUpdatingAnUnknownFacilityWithAnInvalidBody()
    {
        var response = await PutRawJson(Guid.NewGuid(), FacilityValidationCases.ValidBody(body =>
        {
            body["name"] = "";
            body["capacity"] = -1;
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task ReportEveryInvalidFieldOfTheFacilityAndItsAddressWithoutEchoingTheRejectedValues()
    {
        var createdId = await CreateFacilityWithStands("North");
        var before = await GetFacility(createdId);
        var invalid = FacilityValidationCases.ValidBody(FacilityValidationCases.SeveralInvalidFields);

        var created = await PostRawJson(invalid);
        var updated = await PutRawJson(createdId, invalid);

        foreach (var (response, key) in new[] { (created, FacilityValidationCases.CreateValidationKey), (updated, FacilityValidationCases.UpdateValidationKey) })
        {
            var problem = await FacilityValidationCases.ShouldBeAValidationProblem(response, key, FacilityValidationCases.SeveralInvalidFieldNames);
            problem.ToString().Should().NotContain(FacilityValidationCases.TooLongText);
        }

        (await GetFacility(createdId)).Should().BeEquivalentTo(before);
    }

    [TestCase("POST", """{"name":"Arena",""", null)]
    [TestCase("POST", "UNKNOWN_TYPE", "Type")]
    [TestCase("PUT", """{"name":"Arena",""", null)]
    [TestCase("PUT", "UNKNOWN_TYPE", "Type")]
    public async Task ReturnTheValidationFormatForBodiesThatCannotBeBound(string method, string body, string? field)
    {
        var json = body == "UNKNOWN_TYPE"
            ? FacilityValidationCases.ValidBody(b => b["type"] = FacilityValidationCases.UnknownTypeText)
            : body;
        var response = method == "POST"
            ? await PostRawJson(json)
            : await PutRawJson(await CreateFacilityWithStands("North"), json);

        var key = method == "POST" ? FacilityValidationCases.CreateValidationKey : FacilityValidationCases.UpdateValidationKey;
        var problem = await FacilityValidationCases.ShouldBeAValidationProblem(response, key, field is null ? [] : [field]);
        ((JObject)problem["Extensions"]!["Errors"]!).Properties().Should().NotBeEmpty();
        problem.ToString().Should().NotContain(FacilityValidationCases.UnknownTypeText);
    }

    [Test]
    public async Task KeepReadingAndUpdatingAStoredFacilityThatBreaksTheRules()
    {
        var legacy = await FacilityValidationCases.InsertALegacyFacilityThatBreaksTheRules();

        var stored = await GetFacility(legacy.Id);
        stored.Name.Should().Be(legacy.Name);
        stored.Capacity.Should().Be(legacy.Capacity);
        stored.BuildDate.Should().Be(legacy.BuildDate);
        stored.Address.PostalCode.Should().Be(legacy.PostalCode);
        var all = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Uris.GetAllFacilities()));
        all.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await all.Content.ReadFromJsonAsync<List<SearchFacilityResult>>(TestJson.Options);
        listed.Should().ContainSingle(facility => facility.Id == legacy.Id).Which.Should().BeEquivalentTo(stored);

        var response = await PutRawJson(legacy.Id, FacilityValidationCases.ValidBody());

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await GetFacility(legacy.Id)).Name.Should().Be(FacilityValidationCases.ValidName);
    }

    // --- Step 03b: domain validation of the nested stands (OPT-037: one representative per rule type) ---

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.InvalidNestedStands))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.NullStandRepresentative))]
    public async Task RejectTheCreationOfAFacilityWithAnInvalidStandWithoutSavingIt(string field, Action<JObject> change)
    {
        var before = await FacilityValidationCases.CountStoredRows();

        var response = await PostRawJson(FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.CreateValidationKey, field);
        (await FacilityValidationCases.CountStoredRows()).Should().Be(before);
    }

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.InvalidNestedStands))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.NullStandRepresentative))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.TooManyStands))]
    public async Task RejectTheUpdateOfAFacilityWithInvalidStandsAndChangeNothing(string field, Action<JObject> change)
    {
        var createdId = await CreateFacilityWithStands("North", "South");
        var before = await GetFacility(createdId);

        var response = await PutRawJson(createdId, FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, field);
        (await GetFacility(createdId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task CreateAndUpdateAFacilityWithStandsAtTheLimits()
    {
        var longest = new string('s', 100);
        var created = await PostRawJson(FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, (longest, 0), ("North", 1))));
        var createdRaw = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, createdRaw);
        var id = JsonConvert.DeserializeObject<Guid>(createdRaw);
        (await GetFacility(id)).Stands.Select(stand => (stand.StandName, stand.StandCapacity))
            .Should().BeEquivalentTo([(longest, 0), ("North", 1)]);

        var updatedName = new string('u', 100);
        var updated = await PutRawJson(id, FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, (updatedName, 0))));

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        (await GetFacility(id)).Stands.Select(stand => (stand.StandName, stand.StandCapacity))
            .Should().BeEquivalentTo([(longest, 0), ("North", 1), (updatedName, 0)]);
    }

    [Test]
    public async Task ReportEveryInvalidFieldOfTheFacilityItsAddressAndItsStandsWithoutEchoingTheRejectedValues()
    {
        var createdId = await CreateFacilityWithStands("North");
        var before = await GetFacility(createdId);
        var invalid = FacilityValidationCases.ValidBody(body =>
        {
            FacilityValidationCases.SeveralInvalidFields(body);
            FacilityValidationCases.SetStands(body, (FacilityValidationCases.TooLongText, 10), ("Valid", -1));
        });
        string[] fields = [.. FacilityValidationCases.SeveralInvalidFieldNames, "Stands[0].StandName", "Stands[1].StandCapacity"];

        var created = await PostRawJson(invalid);
        var updated = await PutRawJson(createdId, invalid);

        foreach (var (response, key) in new[] { (created, FacilityValidationCases.CreateValidationKey), (updated, FacilityValidationCases.UpdateValidationKey) })
        {
            var problem = await FacilityValidationCases.ShouldBeAValidationProblem(response, key, fields);
            problem.ToString().Should().NotContain(FacilityValidationCases.TooLongText);
        }

        (await GetFacility(createdId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task RejectFiveSentStandsForAFacilityThatHasFourAndChangeNothing()
    {
        var created = await PostRawJson(FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, FacilityValidationCases.FourStoredStands)));
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        var before = await GetFacility(id);

        var response = await PutRawJson(id, FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, FacilityValidationCases.FiveSentStands)));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, "Stands");
        (await GetFacility(id)).Should().BeEquivalentTo(before);
    }

    private async Task<HttpResponseMessage> PostRawJson(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Uris.CreateFacility())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutRawJson(Guid id, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, Uris.UpdateFacility(id.ToString()))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }
}

/// <summary>
/// AC3a.11 (OPT-035): the cases where the <see cref="TestFixture"/> host (<c>AddControllers</c>, DataAnnotations and
/// implicit required) and production (<c>AddMvcCore</c>) diverge, checked on the production pipeline
/// (<see cref="StartupTestServer"/>).
/// </summary>
public class FacilityValidationInTheProductionPipelineShould
{
    private const string FacilitiesPath = "/api/v1.0/facilities/";
    private StartupTestServer _server;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => _server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _server.DisposeAsync();

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.DivergentBodies))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.DivergentStandBodies))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.NullStands))]
    public async Task RejectTheCreationWithTheOperationKeyAndFieldWithoutSaving(string field, Action<JObject> change)
    {
        var before = await FacilityValidationCases.CountStoredRows();

        var response = await Send(HttpMethod.Post, FacilitiesPath, FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.CreateValidationKey, field);
        (await FacilityValidationCases.CountStoredRows()).Should().Be(before);
    }

    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.DivergentBodies))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.DivergentStandBodies))]
    [TestCaseSource(typeof(FacilityValidationCases), nameof(FacilityValidationCases.NullStands))]
    public async Task RejectTheUpdateWithTheOperationKeyAndFieldWithoutChanges(string field, Action<JObject> change)
    {
        var created = await Send(HttpMethod.Post, FacilitiesPath, FacilityValidationCases.ValidBody());
        var createdRaw = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, createdRaw);
        var id = JsonConvert.DeserializeObject<Guid>(createdRaw);
        var before = await _server.Client.GetStringAsync($"{FacilitiesPath}{id}");

        var response = await Send(HttpMethod.Put, $"{FacilitiesPath}{id}", FacilityValidationCases.ValidBody(change));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, field);
        (await _server.Client.GetStringAsync($"{FacilitiesPath}{id}")).Should().Be(before);
    }

    [Test]
    public async Task RejectFiveSentStandsForAFacilityThatHasFourInTheDomainAndChangeNothing()
    {
        var created = await Send(HttpMethod.Post, FacilitiesPath, FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, FacilityValidationCases.FourStoredStands)));
        var createdRaw = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, createdRaw);
        var id = JsonConvert.DeserializeObject<Guid>(createdRaw);
        var before = await _server.Client.GetStringAsync($"{FacilitiesPath}{id}");

        var response = await Send(HttpMethod.Put, $"{FacilitiesPath}{id}", FacilityValidationCases.ValidBody(body =>
            FacilityValidationCases.SetStands(body, FacilityValidationCases.FiveSentStands)));

        await FacilityValidationCases.ShouldBeAValidationProblem(response, FacilityValidationCases.UpdateValidationKey, "Stands");
        (await _server.Client.GetStringAsync($"{FacilitiesPath}{id}")).Should().Be(before);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string json) =>
        await _server.Client.SendAsync(new HttpRequestMessage(method, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
}

/// <summary>Shared bodies, cases and assertions of the facility validation tests (step 03a).</summary>
public static class FacilityValidationCases
{
    public const string CreateValidationKey = "Error.Facilities.Create.Validation";
    public const string UpdateValidationKey = "Error.Facilities.Update.Validation";
    public const string ValidName = "Validation Arena";
    public const string UnknownTypeText = "ZZZ_UNKNOWN_TYPE";
    public static readonly string TooLongText = new('a', 101);
    private static DateTime TodayUtc => DateTime.UtcNow.Date;

    private static readonly string[] LongTextAddressFields = ["street", "city", "region", "country"];
    private static readonly string[] ShortTextAddressFields = ["streetNumber", "postalCode"];

    public static string ValidBody(Action<JObject>? change = null)
    {
        var body = new JObject
        {
            ["name"] = ValidName,
            ["active"] = true,
            ["buildDate"] = IsoDate(new DateTime(2020, 7, 20)),
            ["type"] = "Arena",
            ["capacity"] = 18000,
            ["address"] = new JObject
            {
                ["street"] = "Arena Road",
                ["streetNumber"] = "50",
                ["postalCode"] = "08001",
                ["city"] = "Barcelona",
                ["region"] = "Catalonia",
                ["country"] = "Spain",
            },
        };
        change?.Invoke(body);
        return body.ToString(Formatting.None);
    }

    public static IEnumerable<TestCaseData> InvalidBodies()
    {
        TestCaseData Case(string name, string field, Action<JObject> change) =>
            new TestCaseData(field, change).SetArgDisplayNames(name);

        yield return Case("Name empty", "Name", body => body["name"] = "");
        yield return Case("Name whitespace", "Name", body => body["name"] = "   ");
        yield return Case("Name 101 chars", "Name", body => body["name"] = TooLongText);
        yield return Case("Name absent", "Name", body => body.Remove("name"));
        yield return Case("BuildDate 1799-12-31", "BuildDate", body => body["buildDate"] = IsoDate(new DateTime(1799, 12, 31)));
        yield return Case("BuildDate tomorrow", "BuildDate", body => body["buildDate"] = IsoDate(TodayUtc.AddDays(1)));
        yield return Case("BuildDate absent", "BuildDate", body => body.Remove("buildDate"));
        yield return Case("Type 99", "Type", body => body["type"] = 99);
        yield return Case("Capacity -1", "Capacity", body => body["capacity"] = -1);
        yield return Case("Address absent", "Address", body => body.Remove("address"));
        foreach (var field in LongTextAddressFields)
        {
            foreach (var testCase in AddressTextCases(field, TooLongText)) yield return testCase;
        }

        foreach (var field in ShortTextAddressFields)
        {
            foreach (var testCase in AddressTextCases(field, new string('1', 21))) yield return testCase;
        }

        IEnumerable<TestCaseData> AddressTextCases(string field, string tooLong)
        {
            var errorField = $"Address.{Pascal(field)}";
            yield return Case($"{errorField} empty", errorField, body => body["address"]![field] = "");
            yield return Case($"{errorField} whitespace", errorField, body => body["address"]![field] = "   ");
            yield return Case($"{errorField} {tooLong.Length} chars", errorField, body => body["address"]![field] = tooLong);
            yield return Case($"{errorField} absent", errorField, body => ((JObject)body["address"]!).Remove(field));
        }
    }

    /// <summary>
    /// Cases where the <see cref="TestFixture"/> host rejects through model binding ([Required], implicit required)
    /// and production reaches the domain (OPT-035).
    /// </summary>
    public static IEnumerable<TestCaseData> DivergentBodies()
    {
        TestCaseData Case(string name, string field, Action<JObject> change) =>
            new TestCaseData(field, change).SetArgDisplayNames(name);

        yield return Case("Address absent", "Address", body => body.Remove("address"));
        yield return Case("Address null", "Address", body => body["address"] = null);
        yield return Case("Name absent", "Name", body => body.Remove("name"));
        yield return Case("Name null", "Name", body => body["name"] = null);
        foreach (var field in LongTextAddressFields.Concat(ShortTextAddressFields))
        {
            var errorField = $"Address.{Pascal(field)}";
            yield return Case($"{errorField} absent", errorField, body => ((JObject)body["address"]!).Remove(field));
            yield return Case($"{errorField} null", errorField, body => body["address"]![field] = null);
        }
    }

    /// <summary>Step 03b (OPT-037): one representative per stand rule; the invalid stand is the second one.</summary>
    public static IEnumerable<TestCaseData> InvalidNestedStands()
    {
        TestCaseData Case(string name, string field, (string? Name, int Capacity) invalid) =>
            new TestCaseData(field, (Action<JObject>)(body => SetStands(body, ("Valid", 100), invalid))).SetArgDisplayNames(name);

        yield return Case("Stands[1].StandName whitespace", "Stands[1].StandName", ("   ", 100));
        yield return Case("Stands[1].StandName 101 chars", "Stands[1].StandName", (TooLongText, 100));
        yield return Case("Stands[1].StandCapacity -1", "Stands[1].StandCapacity", ("Valid", -1));
    }

    public static IEnumerable<TestCaseData> TooManyStands()
    {
        yield return new TestCaseData("Stands", (Action<JObject>)(body =>
            SetStands(body, ("A", 1), ("B", 1), ("C", 1), ("D", 1), ("E", 1)))).SetArgDisplayNames("5 stands");
    }

    /// <summary>
    /// Step 03b (AC3b.11): stand cases where the <see cref="TestFixture"/> host rejects through model binding
    /// ([MaxLength(4)], implicit required) and production reaches the domain (OPT-035).
    /// </summary>
    public static IEnumerable<TestCaseData> DivergentStandBodies()
    {
        TestCaseData Case(string name, string field, Action<JObject> change) =>
            new TestCaseData(field, change).SetArgDisplayNames(name);

        yield return Case("5 stands", "Stands", body => SetStands(body, ("A", 1), ("B", 1), ("C", 1), ("D", 1), ("E", 1)));
        yield return Case("Stands[0].StandName absent", "Stands[0].StandName",
            body => body["stands"] = new JArray(new JObject { ["standCapacity"] = 10 }));
        yield return Case("Stands[0].StandName null", "Stands[0].StandName", body => SetStands(body, (null, 10)));
    }


    /// <summary>
    /// TECHDEBT-006 AC1.12 (Q9 a)): a <c>null</c> element in <c>stands</c> is reported by its index, never a 500.
    /// <see cref="NullStandRepresentative"/> is the one case used with the <see cref="TestFixture"/> host (OPT-037).
    /// </summary>
    public static IEnumerable<TestCaseData> NullStands()
    {
        yield return new TestCaseData("Stands[0]", (Action<JObject>)(body => body["stands"] = new JArray(JValue.CreateNull())))
            .SetArgDisplayNames("stands [null]");
        foreach (var testCase in NullStandRepresentative()) yield return testCase;
    }

    public static IEnumerable<TestCaseData> NullStandRepresentative()
    {
        yield return new TestCaseData("Stands[1]", (Action<JObject>)(body =>
        {
            SetStands(body, ("North", 100), ("South", 100));
            ((JArray)body["stands"]!).Insert(1, JValue.CreateNull());
        })).SetArgDisplayNames("null between two valid stands");
    }

    /// <summary>TECHDEBT-006 AC1.13: five stands sent to a facility that already has four.</summary>
    public static readonly (string? Name, int Capacity)[] FourStoredStands = [("A", 1), ("B", 1), ("C", 1), ("D", 1)];

    public static readonly (string? Name, int Capacity)[] FiveSentStands = [("E", 1), ("F", 1), ("G", 1), ("H", 1), ("I", 1)];
    public static void SetStands(JObject body, params (string? Name, int Capacity)[] stands) =>
        body["stands"] = new JArray(stands.Select(stand => new JObject
        {
            ["standName"] = stand.Name,
            ["standCapacity"] = stand.Capacity,
        }));

    public static IEnumerable<(string Name, Action<JObject> Change)> Boundaries()
    {
        yield return ("Name and long address fields at 100, short ones at 20", body =>
        {
            body["name"] = new string('n', 100);
            foreach (var field in LongTextAddressFields) body["address"]![field] = new string(field[0], 100);
            foreach (var field in ShortTextAddressFields) body["address"]![field] = new string('9', 20);
        });
        yield return ("Capacity 0 and BuildDate 1800-01-01", body =>
        {
            body["capacity"] = 0;
            body["buildDate"] = IsoDate(new DateTime(1800, 1, 1));
        });
        yield return ("BuildDate today (UTC)", body => body["buildDate"] = IsoDate(TodayUtc));
    }

    public static void SeveralInvalidFields(JObject body)
    {
        body["name"] = TooLongText;
        body["capacity"] = -1;
        body["buildDate"] = IsoDate(TodayUtc.AddDays(1));
        body["address"]!["street"] = TooLongText;
        body["address"]!["postalCode"] = "";
    }

    public static readonly string[] SeveralInvalidFieldNames = ["Name", "Capacity", "BuildDate", "Address.Street", "Address.PostalCode"];

    public static void ShouldMatchTheSentBody(SearchFacilityResult facility, string json, string because)
    {
        var body = JObject.Parse(json);
        facility.Name.Should().Be(body["name"]!.Value<string>(), because);
        facility.Capacity.Should().Be(body["capacity"]!.Value<int>(), because);
        facility.BuildDate.Date.Should().Be(DateTime.Parse(body["buildDate"]!.Value<string>()!.Substring(0, 10),
            System.Globalization.CultureInfo.InvariantCulture), because);
        facility.Address.Street.Should().Be(body["address"]!["street"]!.Value<string>(), because);
        facility.Address.StreetNumber.Should().Be(body["address"]!["streetNumber"]!.Value<string>(), because);
        facility.Address.PostalCode.Should().Be(body["address"]!["postalCode"]!.Value<string>(), because);
        facility.Address.City.Should().Be(body["address"]!["city"]!.Value<string>(), because);
        facility.Address.Region.Should().Be(body["address"]!["region"]!.Value<string>(), because);
        facility.Address.Country.Should().Be(body["address"]!["country"]!.Value<string>(), because);
    }

    public static async Task<JObject> ShouldBeAValidationProblem(HttpResponseMessage response, string key, params string[] fields)
    {
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);
        var body = JObject.Parse(raw);
        var translationKey = body["Extensions"]?["ErrorTranslationKey"]?.Value<string>();
        translationKey.Should().Be(key, raw);
        (body["Extensions"]?["Errors"] as JObject).Should().NotBeNull(raw);
        var errorFields = ((JObject)body["Extensions"]!["Errors"]!).Properties().Select(property => property.Name);
        if (fields.Length > 0) errorFields.Should().Contain(fields, raw);
        return body;
    }

    public static async Task<(long Facilities, long Addresses)> CountStoredRows() =>
        (await TestFixture.SqlConnectionProviderTest.QueryScalarAsync<long>("SELECT COUNT(*) FROM `Facilities`;"),
            await TestFixture.SqlConnectionProviderTest.QueryScalarAsync<long>("SELECT COUNT(*) FROM `Addresses`;"));

    public sealed record LegacyFacility(Guid Id, string Name, int Capacity, DateTime BuildDate, string PostalCode);

    /// <summary>Inserts by SQL a facility that breaks the creation rules (Q16: stored data is read without validation).</summary>
    public static async Task<LegacyFacility> InsertALegacyFacilityThatBreaksTheRules()
    {
        var legacy = new LegacyFacility(Guid.NewGuid(), new string('x', 150), -5, new DateTime(1700, 1, 1), new string('7', 30));
        var addressId = Guid.NewGuid();
        var provider = TestFixture.SqlConnectionProviderTest;
        await provider.ExecuteAsync(
            "INSERT INTO `Facilities` (Id, Name, Active, BuildDate, Type, Capacity) VALUES (@Id, @Name, 1, @BuildDate, 'Arena', @Capacity);",
            new { legacy.Id, legacy.Name, legacy.BuildDate, legacy.Capacity });
        await provider.ExecuteAsync(
            "INSERT INTO `Addresses` (Id, Street, StreetNumber, PostalCode, City, Region, Country) " +
            "VALUES (@Id, '', '1', @PostalCode, 'Legacy City', 'Legacy Region', 'Legacy Country');",
            new { Id = addressId, legacy.PostalCode });
        await provider.ExecuteAsync(
            "INSERT INTO `Facilities_Addresses` (FacilityId, AddressId) VALUES (@FacilityId, @AddressId);",
            new { FacilityId = legacy.Id, AddressId = addressId });
        return legacy;
    }

    private static string Pascal(string field) => char.ToUpperInvariant(field[0]) + field[1..];

    private static string IsoDate(DateTime date) =>
        date.ToString("yyyy-MM-dd'T'00:00:00'Z'", System.Globalization.CultureInfo.InvariantCulture);
}