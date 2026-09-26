using System.Linq.Expressions;
using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class PlayerFeaturesShould
{
    private GafferApiClientTest _client;
    
    private Guid PlayerId = TestDefaultValues.EmptyPlayerId;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime BirthDate = TestDefaultValues.DefaultBirthDate;
    private const Gender Gender = TestDefaultValues.DefaultGender;
    private Player Player1;
    private Player Player2;
    private Player Player3;

    [SetUp]
    public async Task SetUp()
    {
        _client = (GafferApiClientTest) TestFixture.GafferApiClientTest;
        Player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, BirthDate);
        Player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, BirthDate);
        Player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", null, Gender.MALE, BirthDate);
    }

    [Test]
    public async Task GetAPlayer()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var result = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        
        var response = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(result.ToString()));

        response.Should().NotBeNull();
        response.Id.Should().Be(result);
        response.FirstName.Should().Be(Player1.FirstName);
        response.LastName.Should().Be(Player1.LastName);
        response.BirthDate.Should().Be(Player1.Birthdate);
        response.Gender.Should().Be(Player1.Gender);
    }
    
    [Test]
    public async Task ReturnCreatedWithTheLocationOfTheCreatedPlayer()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var request = new HttpRequestMessage(HttpMethod.Post, Uris.CreatePlayer())
        {
            Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(createPlayerRequest),
                System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1.0/players/{createdId}");
        var player = await _client.GetAsync<SearchPlayerResponse>(response.Headers.Location);
        player!.Id.Should().Be(createdId);
        player.FirstName.Should().Be(Player1.FirstName);
    }

    [Test]
    public async Task ReturnNotFoundWhenGettingAnUnknownPlayer()
    {
        var uri = Uris.GetPlayer(Guid.NewGuid().ToString());

        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        body["Status"]!.Value<int>().Should().Be(404);
        body["Instance"]!.Value<string>().Should().Be(uri.AbsolutePath);
        body["Title"]?.Value<string>().Should().BeNull();
        body["Detail"]?.Value<string>().Should().BeNull();
        (body["Extensions"] as JObject)?.ContainsKey("ErrorTranslationKey").Should().BeFalse();
    }

    [Test]
    public async Task DeleteAPlayer()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var result = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        var filter = new SearchPlayersFilter { Name = "Id", Value = $"{PlayerId}" };
        var request = new SearchPlayersRequest();
        request.Filters.Add(filter);

        await _client.DeleteAsync(Uris.DeletePlayer(result.ToString()));
        
        var response = async () => await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(result.ToString()));
        await response.Should().ThrowAsync<Exception>();
    }
    
    [Test]
    public async Task UpdateAPlayer()
    {
        var newFirstName = "AnotherFirstName";
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var result = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        var filter = new SearchPlayersFilter { Name = "Id", Value = $"{PlayerId}" };
        var request = new SearchPlayersRequest();
        request.Filters.Add(filter);
        var updatePlayerRequest = new UpdatePlayerRequest { FirstName = newFirstName };
        
        await _client.UpdateAsync(Uris.UpdatePlayer(result.ToString()), updatePlayerRequest);
        
        var response = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(result.ToString()));
        response.Should().NotBeNull();
        response.FirstName.Should().Be(newFirstName);
    }
    
    [Test]
    public async Task ThrowExceptionIfPlayerIsNotFound()
    {
        var newFirstName = "AnotherFirstName";
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        var updatePlayerRequest = new UpdatePlayerRequest { FirstName = newFirstName };
        
        var actionResult = async () => await _client.UpdateAsync(Uris.UpdatePlayer(Guid.NewGuid().ToString()), updatePlayerRequest);

        actionResult.Should().ThrowAsync<Exception>().Result.And.Message.Should().Contain("NotFound");
    }
}