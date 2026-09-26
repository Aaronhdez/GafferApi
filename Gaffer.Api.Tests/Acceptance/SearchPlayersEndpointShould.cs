using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Clients;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class SearchPlayersEndpointShould
{
    private const string FilterNotPermittedKey = "Error.Search.FilterNotPermitted";
    private IGafferApiClient _client;

    [SetUp]
    public async Task SetUp()
    {
        _client = TestFixture.GafferApiClientTest;
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
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        body["Status"]!.Value<int>().Should().Be(400);
        body["Title"]!.Value<string>().Should().Be(FilterNotPermittedKey);
        body["Detail"]!.Value<string>().Should().Be("FilterNotPermitted");
        body["Instance"]!.Value<string>().Should().Be(Uris.SearchPlayers().AbsolutePath);
        body["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be(FilterNotPermittedKey);
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

    private async Task CreatePlayer(string firstName, string lastName)
    {
        var request = new CreatePlayerRequest(firstName, lastName, null, TestDefaultValues.DefaultBirthDate, Gender.MALE);
        await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), request);
    }

    private async Task<HttpResponseMessage> SendSearch(string fieldName, string value)
    {
        var request = new SearchPlayersRequest();
        request.Filters.Add(new SearchPlayersFilter { Name = fieldName, Value = value });
        return await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, Uris.SearchPlayers())
        {
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        });
    }
}
