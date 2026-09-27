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
        var created = await CreatePlayerWithKnownValues();

        var response = await PutRawJson(created, """{"firstName":"X"}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var player = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        player!.FirstName.Should().Be("X");
        player.LastName.Should().Be(KnownLastName);
        player.BirthDate.Should().Be(KnownBirthDate);
        player.Gender.Should().Be(KnownGender);
        player.SecondLastName.Should().BeNull();
    }

    [Test]
    public async Task KeepBirthDateAndGenderWhenUpdatedWithExplicitNulls()
    {
        var created = await CreatePlayerWithKnownValues();

        var response = await PutRawJson(created, """{"firstName":"X","birthDate":null,"gender":null}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var player = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        player!.FirstName.Should().Be("X");
        player.LastName.Should().Be(KnownLastName);
        player.BirthDate.Should().Be(KnownBirthDate);
        player.Gender.Should().Be(KnownGender);
    }

    [Test]
    public async Task UpdateEveryFieldWhenAllFieldsAreSent()
    {
        var created = await CreatePlayerWithKnownValues();

        var response = await PutRawJson(created,
            """{"firstName":"Nuevo","lastName":"Apellido","secondLastName":"Segundo","birthDate":"2001-02-03T00:00:00Z","gender":"OTHER"}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var player = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        player!.FirstName.Should().Be("Nuevo");
        player.LastName.Should().Be("Apellido");
        player.SecondLastName.Should().Be("Segundo");
        player.BirthDate.Should().Be(new DateTime(2001, 2, 3));
        player.Gender.Should().Be(Gender.OTHER);
    }

    [TestCase("""{"secondLastName":"Nuevo"}""", "Nuevo")]
    [TestCase("""{"secondLastName":null}""", null)]
    [TestCase("""{"secondLastName":""}""", null)]
    public async Task ReplaceTheSecondLastNameOnUpdate(string body, string? expectedSecondLastName)
    {
        var created = await CreatePlayerWithKnownValues();

        var response = await PutRawJson(created, body);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var player = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        player!.SecondLastName.Should().Be(expectedSecondLastName);
        player.FirstName.Should().Be(KnownFirstName);
        player.LastName.Should().Be(KnownLastName);
        player.BirthDate.Should().Be(KnownBirthDate);
        player.Gender.Should().Be(KnownGender);
    }

    [Test]
    public async Task ReturnNotFoundWhenUpdatingAnUnknownPlayer()
    {
        var response = await PutRawJson(Guid.NewGuid(), """{"firstName":"X"}""");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        body["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be("Error.Players.Update.PlayerNotFound");
    }

    [Test]
    public async Task ReturnNotFoundWhenDeletingAnUnknownPlayer()
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Uris.DeletePlayer(Guid.NewGuid().ToString())));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());
        body["Extensions"]!["ErrorTranslationKey"]!.Value<string>().Should().Be("Error.Players.Delete.PlayerNotFound");
    }

    private const string KnownFirstName = "Alexia";
    private const string KnownLastName = "Putellas";
    private const Gender KnownGender = Gender.FEMALE;
    private static readonly DateTime KnownBirthDate = new(1994, 2, 4);

    private async Task<Guid> CreatePlayerWithKnownValues()
    {
        var createPlayerRequest = new CreatePlayerRequest(KnownFirstName, KnownLastName,
            TestDefaultValues.DefaultSecondLastName, KnownBirthDate, KnownGender);
        return await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
    }

    private async Task<HttpResponseMessage> PutRawJson(Guid playerId, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, Uris.UpdatePlayer(playerId.ToString()))
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
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