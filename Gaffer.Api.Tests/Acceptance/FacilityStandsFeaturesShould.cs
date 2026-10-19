using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Queries;
using Gaffer.Api.Queries.Facilities;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class FacilityStandsFeaturesShould {
    private IGafferApiClient client;

    [SetUp]
    public void SetUp() => client = TestFixture.GafferApiClientTest;

    [Test]
    public async Task ListTheStandsOfAFacilityAndAnEmptyListWhenThereAreNone() {
        var withStands = await CreateFacilityWithStands("North", "South");
        var withoutStands = await CreateFacilityWithStands();

        var listed = await GetStands(withStands);
        var empty = await GetStands(withoutStands);

        listed.Select(stand => stand.StandName).Should().BeEquivalentTo("North", "South");
        empty.Should().BeEmpty();
    }

    [Test]
    public async Task ReadAStandOfAFacility() {
        var facilityId = await CreateFacilityWithStands("North");
        var stand = (await GetStands(facilityId)).Single();

        var response = await Send(HttpMethod.Get, Uris.FacilityStand($"{facilityId}", $"{stand.Id}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var read = await response.Content.ReadFromJsonAsync<SearchFacilityStandResult>(TestJson.Options);
        read.Should().BeEquivalentTo(new SearchFacilityStandResult(stand.Id, "North", 4500));
    }

    [Test]
    public async Task CreateAStandReturningItsServerGeneratedIdAndLocation() {
        var facilityId = await CreateFacilityWithStands("North");

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = "East", StandCapacity = 3000 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var standId = await response.Content.ReadFromJsonAsync<Guid>();
        standId.Should().NotBeEmpty();
        response.Headers.Location!.ToString().Should().EndWith($"/facilities/{facilityId}/stands/{standId}");
        var created = await Send(HttpMethod.Get, response.Headers.Location);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetStands(facilityId)).Should().HaveCount(2);
    }

    [Test]
    public async Task IgnoreAnIdSentInTheCreationBody() {
        var facilityId = await CreateFacilityWithStands();
        var requested = Guid.NewGuid();

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { Id = requested, StandName = "East", StandCapacity = 3000 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<Guid>()).Should().NotBe(requested);
    }

    [Test]
    public async Task RejectAFifthStandWithoutChangingTheFacility() {
        var facilityId = await CreateFacilityWithStands("A", "B", "C", "D");
        var before = await GetStands(facilityId);

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = "E", StandCapacity = 1000 });

        await AssertProblem(response, HttpStatusCode.BadRequest, "Error.Facilities.Stands.Create.MaxStandsExceeded");
        (await GetStands(facilityId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task ReplaceTheNameAndCapacityOfAStandKeepingItsId() {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var north = (await GetStands(facilityId)).Single(stand => stand.StandName == "North");

        var response = await Send(HttpMethod.Put, Uris.FacilityStand($"{facilityId}", $"{north.Id}"), new { StandName = "North Renovated", StandCapacity = 5000 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stands = await GetStands(facilityId);
        stands.Should().HaveCount(2);
        stands.Single(stand => stand.Id == north.Id).Should().BeEquivalentTo(new SearchFacilityStandResult(north.Id, "North Renovated", 5000));
    }

    [Test]
    public async Task DeleteAStandIncludingTheLastOne() {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var stands = await GetStands(facilityId);

        foreach (var stand in stands) {
            var response = await Send(HttpMethod.Delete, Uris.FacilityStand($"{facilityId}", $"{stand.Id}"));
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await GetStands(facilityId)).Should().BeEmpty();
        (await Send(HttpMethod.Get, Uris.GetFacility($"{facilityId}"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AnswerNotFoundWithTheFacilityKeyWhenTheFacilityDoesNotExist() {
        var missing = Guid.NewGuid();
        var body = new { StandName = "East", StandCapacity = 3000 };

        var responses = new[]
        {
            await Send(HttpMethod.Get, Uris.FacilityStands($"{missing}")),
            await Send(HttpMethod.Get, Uris.FacilityStand($"{missing}", $"{Guid.NewGuid()}")),
            await Send(HttpMethod.Post, Uris.FacilityStands($"{missing}"), body),
            await Send(HttpMethod.Put, Uris.FacilityStand($"{missing}", $"{Guid.NewGuid()}"), body),
            await Send(HttpMethod.Delete, Uris.FacilityStand($"{missing}", $"{Guid.NewGuid()}")),
        };

        foreach (var response in responses) {
            await AssertProblem(response, HttpStatusCode.NotFound, "Error.Facilities.Stands.FacilityNotFound");
        }
    }

    [Test]
    public async Task AnswerNotFoundWithTheStandKeyForMissingOrForeignStandsWithoutChangingThem() {
        var facilityId = await CreateFacilityWithStands("North");
        var otherFacilityId = await CreateFacilityWithStands("Foreign");
        var foreign = (await GetStands(otherFacilityId)).Single();
        var body = new { StandName = "Hijacked", StandCapacity = 1 };

        var responses = new[]
        {
            await Send(HttpMethod.Get, Uris.FacilityStand($"{facilityId}", $"{Guid.NewGuid()}")),
            await Send(HttpMethod.Get, Uris.FacilityStand($"{facilityId}", $"{foreign.Id}")),
            await Send(HttpMethod.Put, Uris.FacilityStand($"{facilityId}", $"{foreign.Id}"), body),
            await Send(HttpMethod.Delete, Uris.FacilityStand($"{facilityId}", $"{foreign.Id}")),
            await Send(HttpMethod.Delete, Uris.FacilityStand($"{facilityId}", $"{Guid.NewGuid()}")),
        };

        foreach (var response in responses) {
            await AssertProblem(response, HttpStatusCode.NotFound, "Error.Facilities.Stands.StandNotFound");
        }
        (await GetStands(otherFacilityId)).Should().BeEquivalentTo([foreign]);
    }

    [Test]
    public async Task NeverExceedFourStandsWhenCreatingThemConcurrently() {
        const int attempts = 10;
        const int concurrentRequests = 4;
        for (var attempt = 0; attempt < attempts; attempt++) {
            var facilityId = await CreateFacilityWithStands("A", "B", "C");
            var start = new TaskCompletionSource();
            var requests = Enumerable.Range(0, concurrentRequests).Select(async i => {
                await start.Task;
                return await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = $"Extra {i}", StandCapacity = 1000 });
            }).ToList();

            start.SetResult();
            var responses = await Task.WhenAll(requests);

            responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest).Should().Be(concurrentRequests - 1);
            (await GetStands(facilityId)).Should().HaveCount(4);
        }
    }

    // --- Step 03b: domain validation of the stand sub-resource (OPT-037: one representative per rule type) ---

    [TestCaseSource(typeof(StandValidationCases), nameof(StandValidationCases.InvalidStands))]
    public async Task RejectAnInvalidStandWithoutChangingTheStands(string method, string field, object body) {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var before = await GetStands(facilityId);
        var uri = method == "POST"
            ? Uris.FacilityStands($"{facilityId}")
            : Uris.FacilityStand($"{facilityId}", $"{before[0].Id}");

        var response = await Send(new HttpMethod(method), uri, body);

        await FacilityValidationCases.ShouldBeAValidationProblem(response, StandValidationCases.StandsValidationKey, field);
        (await GetStands(facilityId)).Should().BeEquivalentTo(before);
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    public async Task RejectAnInvalidStandBeforeLookingForTheFacility(string method) {
        var unknownFacility = Guid.NewGuid();
        var uri = method == "POST"
            ? Uris.FacilityStands($"{unknownFacility}")
            : Uris.FacilityStand($"{unknownFacility}", $"{Guid.NewGuid()}");

        var response = await Send(new HttpMethod(method), uri, new { StandName = "East", StandCapacity = -1 });

        await FacilityValidationCases.ShouldBeAValidationProblem(response, StandValidationCases.StandsValidationKey, "standCapacity");
    }

    [Test]
    public async Task RejectAStandNameLongerThanItsColumnWithAValidationError() {
        var facilityId = await CreateFacilityWithStands("North");
        var before = await GetStands(facilityId);

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = new string('x', 300), StandCapacity = 1000 });

        await FacilityValidationCases.ShouldBeAValidationProblem(response, StandValidationCases.StandsValidationKey, "standName");
        (await GetStands(facilityId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task CreateAndReplaceAStandWithValuesAtTheLimits() {
        var facilityId = await CreateFacilityWithStands("North");
        var longest = new string('s', 100);

        var created = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = longest, StandCapacity = 0 });

        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var standId = await created.Content.ReadFromJsonAsync<Guid>();
        (await GetStands(facilityId)).Should().ContainEquivalentOf(new SearchFacilityStandResult(standId, longest, 0));

        var replacedName = new string('r', 100);
        var replaced = await Send(HttpMethod.Put, Uris.FacilityStand($"{facilityId}", $"{standId}"), new { StandName = replacedName, StandCapacity = 0 });

        replaced.StatusCode.Should().Be(HttpStatusCode.NoContent, await replaced.Content.ReadAsStringAsync());
        (await GetStands(facilityId)).Should().ContainEquivalentOf(new SearchFacilityStandResult(standId, replacedName, 0));
    }

    [TestCase("POST", """{"standName":"East",""", null)]
    [TestCase("POST", """{"standName":"East","standCapacity":"ZZZ_NOT_A_NUMBER"}""", "standCapacity")]
    [TestCase("PUT", """{"standName":"East",""", null)]
    [TestCase("PUT", """{"standName":"East","standCapacity":"ZZZ_NOT_A_NUMBER"}""", "standCapacity")]
    public async Task ReturnTheValidationFormatForStandBodiesThatCannotBeBound(string method, string json, string? field) {
        var facilityId = await CreateFacilityWithStands("North");
        var stands = await GetStands(facilityId);
        var uri = method == "POST"
            ? Uris.FacilityStands($"{facilityId}")
            : Uris.FacilityStand($"{facilityId}", $"{stands[0].Id}");
        var request = new HttpRequestMessage(new HttpMethod(method), uri) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        var problem = await FacilityValidationCases.ShouldBeAValidationProblem(response, StandValidationCases.StandsValidationKey, field is null ? [] : [field]);
        ProblemJson.ErrorsOf(problem).Properties().Should().NotBeEmpty();
        problem.ToString().Should().NotContain("ZZZ_NOT_A_NUMBER");
        (await GetStands(facilityId)).Should().BeEquivalentTo(stands);
    }

    [Test]
    public async Task KeepReadingKeepingAndDeletingAStoredStandThatBreaksTheRules() {
        var facilityId = await CreateFacilityWithStands("North");
        var legacyStandId = await StandValidationCases.InsertALegacyStandWithANegativeCapacity(facilityId);

        var stands = await GetStands(facilityId);
        stands.Should().ContainEquivalentOf(new SearchFacilityStandResult(legacyStandId, StandValidationCases.LegacyStandName, StandValidationCases.LegacyStandCapacity));

        var updated = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, Uris.UpdateFacility($"{facilityId}")) {
            Content = new StringContent(FacilityValidationCases.ValidBody(), Encoding.UTF8, "application/json")
        });

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        (await GetStands(facilityId)).Should().BeEquivalentTo(stands);

        var deleted = await Send(HttpMethod.Delete, Uris.FacilityStand($"{facilityId}", $"{legacyStandId}"));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetStands(facilityId)).Select(stand => stand.Id).Should().NotContain(legacyStandId);
    }

    private async Task<List<SearchFacilityStandResult>> GetStands(Guid facilityId) {
        var response = await Send(HttpMethod.Get, Uris.FacilityStands($"{facilityId}"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stands = await response.Content.ReadFromJsonAsync<List<SearchFacilityStandResult>>(TestJson.Options);
        stands.Should().NotBeNull();
        return stands!;
    }

    private async Task<Guid> CreateFacilityWithStands(params string[] standNames) {
        var requestBody = new {
            Name = "City Arena",
            Active = true,
            BuildDate = new DateTime(2020, 7, 20),
            Type = "Arena",
            Capacity = 18000,
            Address = new {
                Street = "Arena Road",
                StreetNumber = "50",
                PostalCode = "08001",
                City = "Barcelona",
                Region = "Catalonia",
                Country = "Spain"
            },
            Stands = standNames.Select(name => new { StandName = name, StandCapacity = 4500 }).ToArray()
        };
        var response = await Send(HttpMethod.Post, Uris.CreateFacility(), requestBody);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string translationKey) {
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, raw);
        ProblemJson.TranslationKeyOf(ProblemJson.Parse(raw)).Should().Be(translationKey);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, Uri uri, object? body = null) {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null) {
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        }
        return await client.SendAsync(request);
    }
}

/// <summary>
/// AC3b.11 (OPT-035): stand bodies where the <see cref="TestFixture"/> host (implicit required of <c>StandName</c>)
/// and production (<c>AddMvcCore</c>) diverge, checked on the production pipeline (<see cref="StartupTestServer"/>).
/// </summary>
public class FacilityStandValidationInTheProductionPipelineShould {
    private const string FacilitiesPath = "/api/v1.0/facilities/";
    private StartupTestServer server;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() => server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await server.DisposeAsync();

    [TestCase("POST", """{"standCapacity":10}""", TestName = "{m}(POST, StandName absent)")]
    [TestCase("POST", """{"standName":null,"standCapacity":10}""", TestName = "{m}(POST, StandName null)")]
    [TestCase("PUT", """{"standCapacity":10}""", TestName = "{m}(PUT, StandName absent)")]
    [TestCase("PUT", """{"standName":null,"standCapacity":10}""", TestName = "{m}(PUT, StandName null)")]
    public async Task RejectAStandWithoutNameWithTheStandsKeyAndFieldWithoutChanges(string method, string json) {
        var facilityId = await CreateFacility();
        var standsPath = $"{FacilitiesPath}{facilityId}/stands";
        var before = await server.Client.GetStringAsync(standsPath);
        var standId = ((JObject)JArray.Parse(before)[0]).GetValue("id", StringComparison.OrdinalIgnoreCase)!.Value<string>();
        var path = method == "POST" ? standsPath : $"{standsPath}/{standId}";

        var response = await Send(new HttpMethod(method), path, json);

        await FacilityValidationCases.ShouldBeAValidationProblem(response, StandValidationCases.StandsValidationKey, "standName");
        (await server.Client.GetStringAsync(standsPath)).Should().Be(before);
    }

    private async Task<Guid> CreateFacility() {
        var created = await Send(HttpMethod.Post, FacilitiesPath,
            FacilityValidationCases.ValidBody(body => FacilityValidationCases.SetStands(body, ("North", 100))));
        var raw = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, raw);
        return JsonConvert.DeserializeObject<Guid>(raw);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string json) =>
        await server.Client.SendAsync(new HttpRequestMessage(method, path) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
}

/// <summary>Shared cases of the stand sub-resource validation tests (step 03b).</summary>
public static class StandValidationCases {
    public const string StandsValidationKey = "Error.Facilities.Stands.Validation";
    public const string LegacyStandName = "Legacy Stand";
    public const int LegacyStandCapacity = -5;

    /// <summary>OPT-037: one representative per rule type and operation.</summary>
    public static IEnumerable<TestCaseData> InvalidStands() {
        foreach (var method in new[] { "POST", "PUT" }) {
            yield return new TestCaseData(method, "standName", new { StandName = "   ", StandCapacity = 10 })
                .SetArgDisplayNames(method, "StandName whitespace");
            yield return new TestCaseData(method, "standName", new { StandName = FacilityValidationCases.TooLongText, StandCapacity = 10 })
                .SetArgDisplayNames(method, "StandName 101 chars");
            yield return new TestCaseData(method, "standCapacity", new { StandName = "East", StandCapacity = -1 })
                .SetArgDisplayNames(method, "StandCapacity -1");
        }
    }

    /// <summary>Inserts by SQL a stand with a negative capacity linked to the facility (Q16: read without validation).</summary>
    public static async Task<Guid> InsertALegacyStandWithANegativeCapacity(Guid facilityId) {
        var standId = Guid.NewGuid();
        var provider = TestFixture.SqlConnectionProviderTest;
        await provider.ExecuteAsync(
            "INSERT INTO `Stands` (Id, StandName, StandCapacity) VALUES (@Id, @StandName, @StandCapacity);",
            new { Id = standId, StandName = LegacyStandName, StandCapacity = LegacyStandCapacity });
        await provider.ExecuteAsync(
            "INSERT INTO `Facilities_Stands` (FacilityId, StandId) VALUES (@FacilityId, @StandId);",
            new { FacilityId = facilityId, StandId = standId });
        return standId;
    }
}
