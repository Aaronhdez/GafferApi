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
        var body = await ProblemJson.Read(response);
        body["status"]!.Value<int>().Should().Be(404);
        body["instance"]!.Value<string>().Should().Be(uri.AbsolutePath);
        body["title"]!.Value<string>().Should().BeNull();
        body["detail"]!.Value<string>().Should().BeNull();
        body.ContainsKey(ProblemJson.ErrorTranslationKey).Should().BeFalse();
        body.ContainsKey(ProblemJson.Errors).Should().BeFalse();
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
        player.SecondLastName.Should().Be(TestDefaultValues.DefaultSecondLastName);
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
    [TestCase("""{"secondLastName":null}""", TestDefaultValues.DefaultSecondLastName)]
    [TestCase("""{"firstName":"Alexia"}""", TestDefaultValues.DefaultSecondLastName)]
    [TestCase("""{}""", TestDefaultValues.DefaultSecondLastName)]
    public async Task ReplaceTheSecondLastNameOnlyWhenAValueIsSent(string body, string? expectedSecondLastName)
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
        var body = await ProblemJson.Read(response);
        ProblemJson.TranslationKeyOf(body).Should().Be("Error.Players.Update.PlayerNotFound");
    }

    [Test]
    public async Task ReturnNotFoundWhenDeletingAnUnknownPlayer()
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, Uris.DeletePlayer(Guid.NewGuid().ToString())));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ProblemJson.Read(response);
        ProblemJson.TranslationKeyOf(body).Should().Be("Error.Players.Delete.PlayerNotFound");
    }

    private const string CreateValidationKey = "Error.Players.Create.Validation";
    private const string UpdateValidationKey = "Error.Players.Update.Validation";
    private static readonly string TooLongText = new('a', 101);
    private static DateTime TodayUtc => DateTime.UtcNow.Date;

    public static IEnumerable<TestCaseData> InvalidCreateBodies()
    {
        TestCaseData Case(string name, string field, Action<JObject> change) =>
            new TestCaseData(field, change).SetName($"RejectTheCreationOfAPlayerWith({name})");

        yield return Case("FirstName empty", "firstName", body => body["firstName"] = "");
        yield return Case("FirstName whitespace", "firstName", body => body["firstName"] = "   ");
        yield return Case("FirstName 101 chars", "firstName", body => body["firstName"] = TooLongText);
        yield return Case("FirstName absent", "firstName", body => body.Remove("firstName"));
        yield return Case("LastName empty", "lastName", body => body["lastName"] = "");
        yield return Case("LastName whitespace", "lastName", body => body["lastName"] = "   ");
        yield return Case("LastName 101 chars", "lastName", body => body["lastName"] = TooLongText);
        yield return Case("SecondLastName empty", "secondLastName", body => body["secondLastName"] = "");
        yield return Case("SecondLastName whitespace", "secondLastName", body => body["secondLastName"] = "   ");
        yield return Case("SecondLastName 101 chars", "secondLastName", body => body["secondLastName"] = TooLongText);
        yield return Case("BirthDate 1899-12-31", "birthDate", body => body["birthDate"] = IsoDate(new DateTime(1899, 12, 31)));
        yield return Case("BirthDate tomorrow", "birthDate", body => body["birthDate"] = IsoDate(TodayUtc.AddDays(1)));
        yield return Case("BirthDate one day under 16", "birthDate", body => body["birthDate"] = IsoDate(TodayUtc.AddYears(-16).AddDays(1)));
        yield return Case("BirthDate absent", "birthDate", body => body.Remove("birthDate"));
        yield return Case("Gender 99", "gender", body => body["gender"] = 99);
    }

    [TestCaseSource(nameof(InvalidCreateBodies))]
    public async Task RejectTheCreationOfAnInvalidPlayerWithoutSavingIt(string field, Action<JObject> change)
    {
        var playersBefore = await CountPlayers();

        var response = await PostRawJson(ValidCreateBody(change));

        await ShouldBeAValidationProblem(response, CreateValidationKey, field);
        (await CountPlayers()).Should().Be(playersBefore);
    }

    public static IEnumerable<TestCaseData> BoundaryCreateBodies()
    {
        TestCaseData Case(string name, Action<JObject> change) =>
            new TestCaseData(change).SetName($"CreateAPlayerAtTheLimit({name})");

        yield return Case("names of 100 chars", body =>
        {
            body["firstName"] = new string('f', 100);
            body["lastName"] = new string('l', 100);
            body["secondLastName"] = new string('s', 100);
        });
        yield return Case("BirthDate 1900-01-01", body => body["birthDate"] = IsoDate(new DateTime(1900, 1, 1)));
        yield return Case("exactly 16 years old", body => body["birthDate"] = IsoDate(TodayUtc.AddYears(-16)));
        yield return Case("SecondLastName absent", body => body.Remove("secondLastName"));
    }

    [TestCaseSource(nameof(BoundaryCreateBodies))]
    public async Task CreateAPlayerWhoseValuesAreAtTheLimits(Action<JObject> change)
    {
        var sent = JObject.Parse(ValidCreateBody(change));

        var response = await PostRawJson(sent.ToString(Newtonsoft.Json.Formatting.None));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var createdId = await response.Content.ReadFromJsonAsync<Guid>();
        var player = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(createdId.ToString()));
        player!.FirstName.Should().Be(sent["firstName"]!.Value<string>());
        player.LastName.Should().Be(sent["lastName"]!.Value<string>());
        player.SecondLastName.Should().Be(sent["secondLastName"]?.Value<string>());
        player.BirthDate.Should().Be(sent["birthDate"]!.Value<DateTime>().Date);
        player.Gender.Should().Be(Gender.FEMALE);
    }

    public static IEnumerable<TestCaseData> InvalidPartialUpdateBodies()
    {
        TestCaseData Case(string name, string field, string body) =>
            new TestCaseData(field, body).SetName($"RejectAPartialUpdateWith({name})");

        yield return Case("FirstName empty", "firstName", """{"firstName":"","lastName":"Valido"}""");
        yield return Case("LastName whitespace", "lastName", """{"lastName":"   ","firstName":"Valido"}""");
        yield return Case("BirthDate under 16", "birthDate",
            $$"""{"birthDate":"{{IsoDate(TodayUtc.AddYears(-15))}}","firstName":"Valido"}""");
        yield return Case("Gender 99", "gender", """{"gender":99,"firstName":"Valido"}""");
        yield return Case("SecondLastName empty", "secondLastName", """{"secondLastName":"","firstName":"Valido"}""");
        yield return Case("SecondLastName whitespace", "secondLastName", """{"secondLastName":"   "}""");
    }

    [TestCaseSource(nameof(InvalidPartialUpdateBodies))]
    public async Task RejectAPartialUpdateWithAnInvalidFieldAndChangeNothing(string field, string body)
    {
        var created = await CreatePlayerWithKnownValues();
        var before = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));

        var response = await PutRawJson(created, body);

        await ShouldBeAValidationProblem(response, UpdateValidationKey, field);
        var after = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        after.Should().Be(before);
    }

    [Test]
    public async Task KeepThePlayerIdenticalWhenUpdatedWithAnEmptyBody()
    {
        var created = await CreatePlayerWithKnownValues();
        var before = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));

        var response = await PutRawJson(created, "{}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var after = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(created.ToString()));
        after.Should().Be(before);
    }

    [Test]
    public async Task ReturnNotFoundWhenUpdatingAnUnknownPlayerWithAnInvalidBody()
    {
        var response = await PutRawJson(Guid.NewGuid(), """{"firstName":""}""");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        var body = await ProblemJson.Read(response);
        ProblemJson.TranslationKeyOf(body).Should().Be("Error.Players.Update.PlayerNotFound");
    }

    [Test]
    public async Task ReportEveryInvalidFieldOfACreationWithoutEchoingTheRejectedValues()
    {
        var response = await PostRawJson(ValidCreateBody(body =>
        {
            body["firstName"] = TooLongText;
            body["lastName"] = "";
            body["birthDate"] = IsoDate(TodayUtc.AddDays(1));
            body["gender"] = 99;
        }));

        var problem = await ShouldBeAValidationProblem(response, CreateValidationKey, "firstName", "lastName", "birthDate", "gender");
        problem.ToString().Should().NotContain(TooLongText);
    }

    [Test]
    public async Task ReportEveryInvalidFieldOfAPartialUpdate()
    {
        var created = await CreatePlayerWithKnownValues();

        var response = await PutRawJson(created, $$"""{"firstName":"{{TooLongText}}","secondLastName":"","gender":99}""");

        var problem = await ShouldBeAValidationProblem(response, UpdateValidationKey, "firstName", "secondLastName", "gender");
        problem.ToString().Should().NotContain(TooLongText);
    }

    [TestCase("POST", """{"firstName":"Ana",""", null)]
    [TestCase("POST", """{"firstName":"Ana","lastName":"Lopez","birthDate":"1990-01-01T00:00:00Z","gender":"ZZZ_UNKNOWN"}""", "gender")]
    [TestCase("PUT", """{"firstName":"Ana",""", null)]
    [TestCase("PUT", """{"gender":"ZZZ_UNKNOWN"}""", "gender")]
    public async Task ReturnTheValidationFormatForBodiesThatCannotBeBound(string method, string body, string? field)
    {
        var response = method == "POST"
            ? await PostRawJson(body)
            : await PutRawJson(await CreatePlayerWithKnownValues(), body);

        var key = method == "POST" ? CreateValidationKey : UpdateValidationKey;
        var problem = await ShouldBeAValidationProblem(response, key, field is null ? [] : [field]);
        var errors = ProblemJson.ErrorsOf(problem);
        errors.Properties().Should().NotBeEmpty();
        problem.ToString().Should().NotContain("ZZZ_UNKNOWN");
    }

    [Test]
    public async Task KeepReadingAndPartiallyUpdatingAStoredPlayerThatBreaksTheRules()
    {
        var legacyId = Guid.NewGuid();
        var legacyFirstName = new string('x', 150);
        var legacyBirthDate = TodayUtc.AddYears(-10);
        await TestFixture.SqlConnectionProviderTest.ExecuteAsync(
            "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
            "VALUES (@Id, @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);",
            new { Id = legacyId, FirstName = legacyFirstName, LastName = "Legacy", SecondLastName = "", Gender = "OTHER", BirthDate = legacyBirthDate });

        var stored = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(legacyId.ToString()));
        stored!.FirstName.Should().Be(legacyFirstName);
        stored.SecondLastName.Should().BeEmpty();
        stored.BirthDate.Should().Be(legacyBirthDate);
        var searchRequest = new SearchPlayersRequest();
        searchRequest.Filters.Add(new SearchPlayersFilter { Name = "Id", Value = legacyId.ToString() });
        var found = await _client.PostAsync<SearchPlayersRequest, List<SearchPlayerResponse>>(Uris.SearchPlayers(), searchRequest);
        found!.Should().ContainSingle().Which.Should().Be(stored);

        var response = await PutRawJson(legacyId, """{"lastName":"Valido"}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var updated = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(legacyId.ToString()));
        updated.Should().Be(stored with { LastName = "Valido" });
    }

    private static string IsoDate(DateTime date) =>
        date.ToString("yyyy-MM-dd'T'00:00:00'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static string ValidCreateBody(Action<JObject>? change = null)
    {
        var body = new JObject
        {
            ["firstName"] = "Ana",
            ["lastName"] = "Lopez",
            ["secondLastName"] = "Ruiz",
            ["birthDate"] = "1990-01-01T00:00:00Z",
            ["gender"] = "FEMALE",
        };
        change?.Invoke(body);
        return body.ToString(Newtonsoft.Json.Formatting.None);
    }

    private async Task<HttpResponseMessage> PostRawJson(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Uris.CreatePlayer())
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }

    private static async Task<long> CountPlayers() =>
        await TestFixture.SqlConnectionProviderTest.QueryScalarAsync<long>("SELECT COUNT(*) FROM `Players`;");

    private static async Task<JObject> ShouldBeAValidationProblem(HttpResponseMessage response, string key, params string[] fields)
    {
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, raw);
        var body = ProblemJson.Parse(raw);
        var translationKey = ProblemJson.TranslationKeyOf(body);
        translationKey.Should().Be(key, raw);
        (body[ProblemJson.Errors] as JObject).Should().NotBeNull(raw);
        var errorFields = ProblemJson.ErrorsOf(body).Properties().Select(p => p.Name);
        if (fields.Length > 0) errorFields.Should().Contain(fields, raw);
        return body;
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