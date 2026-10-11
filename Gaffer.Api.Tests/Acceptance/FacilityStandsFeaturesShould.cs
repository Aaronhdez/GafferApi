using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Queries;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class FacilityStandsFeaturesShould
{
    private IGafferApiClient _client;

    [SetUp]
    public void SetUp()
    {
        _client = TestFixture.GafferApiClientTest;
    }

    [Test]
    public async Task ListTheStandsOfAFacilityAndAnEmptyListWhenThereAreNone()
    {
        var withStands = await CreateFacilityWithStands("North", "South");
        var withoutStands = await CreateFacilityWithStands();

        var listed = await GetStands(withStands);
        var empty = await GetStands(withoutStands);

        listed.Select(stand => stand.StandName).Should().BeEquivalentTo("North", "South");
        empty.Should().BeEmpty();
    }

    [Test]
    public async Task ReadAStandOfAFacility()
    {
        var facilityId = await CreateFacilityWithStands("North");
        var stand = (await GetStands(facilityId)).Single();

        var response = await Send(HttpMethod.Get, Uris.FacilityStand($"{facilityId}", $"{stand.Id}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var read = await response.Content.ReadFromJsonAsync<SearchFacilityStandResult>(TestJson.Options);
        read.Should().BeEquivalentTo(new SearchFacilityStandResult(stand.Id, "North", 4500));
    }

    [Test]
    public async Task CreateAStandReturningItsServerGeneratedIdAndLocation()
    {
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
    public async Task IgnoreAnIdSentInTheCreationBody()
    {
        var facilityId = await CreateFacilityWithStands();
        var requested = Guid.NewGuid();

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { Id = requested, StandName = "East", StandCapacity = 3000 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<Guid>()).Should().NotBe(requested);
    }

    [Test]
    public async Task RejectAFifthStandWithoutChangingTheFacility()
    {
        var facilityId = await CreateFacilityWithStands("A", "B", "C", "D");
        var before = await GetStands(facilityId);

        var response = await Send(HttpMethod.Post, Uris.FacilityStands($"{facilityId}"), new { StandName = "E", StandCapacity = 1000 });

        await AssertProblem(response, HttpStatusCode.BadRequest, "Error.Facilities.Stands.Create.MaxStandsExceeded");
        (await GetStands(facilityId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task ReplaceTheNameAndCapacityOfAStandKeepingItsId()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var north = (await GetStands(facilityId)).Single(stand => stand.StandName == "North");

        var response = await Send(HttpMethod.Put, Uris.FacilityStand($"{facilityId}", $"{north.Id}"), new { StandName = "North Renovated", StandCapacity = 5000 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stands = await GetStands(facilityId);
        stands.Should().HaveCount(2);
        stands.Single(stand => stand.Id == north.Id).Should().BeEquivalentTo(new SearchFacilityStandResult(north.Id, "North Renovated", 5000));
    }

    [Test]
    public async Task DeleteAStandIncludingTheLastOne()
    {
        var facilityId = await CreateFacilityWithStands("North", "South");
        var stands = await GetStands(facilityId);

        foreach (var stand in stands)
        {
            var response = await Send(HttpMethod.Delete, Uris.FacilityStand($"{facilityId}", $"{stand.Id}"));
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await GetStands(facilityId)).Should().BeEmpty();
        (await Send(HttpMethod.Get, Uris.GetFacility($"{facilityId}"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AnswerNotFoundWithTheFacilityKeyWhenTheFacilityDoesNotExist()
    {
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

        foreach (var response in responses)
        {
            await AssertProblem(response, HttpStatusCode.NotFound, "Error.Facilities.Stands.FacilityNotFound");
        }
    }

    [Test]
    public async Task AnswerNotFoundWithTheStandKeyForMissingOrForeignStandsWithoutChangingThem()
    {
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

        foreach (var response in responses)
        {
            await AssertProblem(response, HttpStatusCode.NotFound, "Error.Facilities.Stands.StandNotFound");
        }
        (await GetStands(otherFacilityId)).Should().BeEquivalentTo([foreign]);
    }

    [Test]
    public async Task NeverExceedFourStandsWhenCreatingThemConcurrently()
    {
        const int attempts = 10;
        const int concurrentRequests = 4;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var facilityId = await CreateFacilityWithStands("A", "B", "C");
            var start = new TaskCompletionSource();
            var requests = Enumerable.Range(0, concurrentRequests).Select(async i =>
            {
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

    private async Task<List<SearchFacilityStandResult>> GetStands(Guid facilityId)
    {
        var response = await Send(HttpMethod.Get, Uris.FacilityStands($"{facilityId}"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stands = await response.Content.ReadFromJsonAsync<List<SearchFacilityStandResult>>(TestJson.Options);
        stands.Should().NotBeNull();
        return stands!;
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
        var response = await Send(HttpMethod.Post, Uris.CreateFacility(), requestBody);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string translationKey)
    {
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, raw);
        JObject.Parse(raw)["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be(translationKey);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, Uri uri, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        }
        return await _client.SendAsync(request);
    }
}
