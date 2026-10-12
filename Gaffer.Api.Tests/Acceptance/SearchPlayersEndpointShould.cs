using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class SearchPlayersEndpointShould
{
    private const string FilterNotPermittedKey = "Error.Search.FilterNotPermitted";
    private IGafferApiClient _client;
    private readonly Dictionary<string, Guid> _createdPlayers = new();

    [SetUp]
    public async Task SetUp()
    {
        _client = TestFixture.GafferApiClientTest;
        _createdPlayers.Clear();
        await ClearPlayers();
        await CreatePlayer("Javier", "Martel");
        await CreatePlayer("Paul", "Gascoigne");
        await CreatePlayer("Shaun", "O'Neill");
    }

    [TearDown]
    public async Task TearDown() => await ClearPlayers();

    private static Task ClearPlayers()
        => new SqlConnectionProvider(TestDefaultValues.TestConnectionString).ClearAsync([DbTableNames.PlayersTable]);

    [Test]
    public async Task ReturnNoPlayersWhenTheFilterValueTriesToInjectSql()
    {
        var response = await SendSearch("FirstName", "x') OR ('1'='1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players.Should().BeEmpty();
    }

    [TestCase("FirstName IN ('x') OR 1=1 --")]
    [TestCase("SecondLastName")]
    [TestCase("Gender")]
    [TestCase("BirthDate")]
    public async Task ReturnBadRequestWhenTheFilterFieldIsNotPermitted(string fieldName)
    {
        var response = await SendSearch(fieldName, "x");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ProblemJson.Read(response);
        body["status"]!.Value<int>().Should().Be(400);
        body["title"]!.Value<string>().Should().Be(FilterNotPermittedKey);
        body["detail"]!.Value<string>().Should().Be("FilterNotPermitted");
        body["instance"]!.Value<string>().Should().Be(Uris.SearchPlayers().AbsolutePath);
        ProblemJson.TranslationKeyOf(body).Should().Be(FilterNotPermittedKey);
    }

    [Test]
    public async Task FindAPlayerWhoseLastNameContainsASingleQuote()
    {
        var response = await SendSearch("LastName", "O'Neill");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players.Should().ContainSingle(p => p.LastName == "O'Neill");
    }

    [Test]
    public async Task MatchFilterFieldNamesIgnoringCase()
    {
        var response = await SendSearch("firstname", "Paul");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players.Should().ContainSingle(p => p.FirstName == "Paul");
    }

    [Test]
    public async Task ReturnEveryPlayerWhenSearchingWithoutFilters()
    {
        var response = await SendSearch();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players!.Select(p => p.Id).Should().BeEquivalentTo(_createdPlayers.Values);
    }

    [Test]
    public async Task ReturnOnlyThePlayerWithTheGivenId()
    {
        var id = _createdPlayers["Gascoigne"];

        var response = await SendSearch("Id", $"{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players.Should().ContainSingle().Which.Should().Be(
            new SearchPlayerResponse(id, "Paul", "Gascoigne", null, TestDefaultValues.DefaultBirthDate, Gender.MALE));
    }

    [TestCase("FirstName", "Paul", "Gascoigne")]
    [TestCase("LastName", "Martel", "Martel")]
    public async Task ReturnOnlyThePlayerMatchingTheNameFilter(string fieldName, string value, string expectedLastName)
    {
        var response = await SendSearch(fieldName, value);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await response.Content.ReadFromJsonAsync<List<SearchPlayerResponse>>(TestJson.Options);
        players.Should().ContainSingle().Which.Id.Should().Be(_createdPlayers[expectedLastName]);
    }

    private async Task CreatePlayer(string firstName, string lastName)
    {
        var request = new CreatePlayerRequest(firstName, lastName, null, TestDefaultValues.DefaultBirthDate, Gender.MALE);
        _createdPlayers[lastName] = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), request);
    }

    private Task<HttpResponseMessage> SendSearch(string fieldName, string value)
        => SendSearch(new SearchPlayersFilter { Name = fieldName, Value = value });

    private async Task<HttpResponseMessage> SendSearch(params SearchPlayersFilter[] filters)
    {
        var request = new SearchPlayersRequest();
        request.Filters.AddRange(filters);
        return await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, Uris.SearchPlayers())
        {
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        });
    }
}
