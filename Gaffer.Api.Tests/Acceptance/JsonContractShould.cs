using System.Net;
using System.Text;
using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.Acceptance;

public class JsonContractShould
{
    private IGafferApiClient _client;

    [SetUp]
    public void SetUp()
    {
        _client = TestFixture.GafferApiClientTest;
    }

    [Test]
    public async Task ReturnAPlayerWithCamelCasePropertiesTextGenderAndUtcBirthDate()
    {
        var id = await CreatePlayer(new
        {
            firstName = "Alexia", lastName = "Putellas", secondLastName = "Segura",
            birthDate = "1994-02-04T00:00:00", gender = "FEMALE"
        });

        var (raw, player) = await GetRaw(Uris.GetPlayer(id.ToString()));

        TestContext.Out.WriteLine(raw);
        player.Properties().Select(property => property.Name).Should()
            .BeEquivalentTo("id", "firstName", "lastName", "secondLastName", "birthDate", "gender");
        player["gender"]!.Type.Should().Be(JTokenType.String);
        player["gender"]!.Value<string>().Should().Be("FEMALE");
        player["birthDate"]!.Value<string>().Should().Be("1994-02-04T00:00:00Z");
    }

    [Test]
    public async Task ReturnAFacilityWithCamelCasePropertiesTextTypeAndUtcBuildDate()
    {
        var id = await CreateFacility(FacilityBody("Arena"));

        var (raw, facility) = await GetRaw(Uris.GetFacility(id.ToString()));

        TestContext.Out.WriteLine(raw);
        facility.Properties().Select(property => property.Name).Should()
            .BeEquivalentTo("id", "name", "active", "buildDate", "type", "capacity", "address", "stands");
        facility["type"]!.Type.Should().Be(JTokenType.String);
        facility["type"]!.Value<string>().Should().Be("Arena");
        facility["buildDate"]!.Value<string>().Should().Be("2020-07-20T00:00:00Z");
    }

    [Test]
    public async Task CreateAPlayerWhoseGenderIsSentAsText()
    {
        var id = await CreatePlayer(PlayerBody("FEMALE"));

        var (_, player) = await GetRaw(Uris.GetPlayer(id.ToString()));

        player["gender"]!.ToString().Should().Be("FEMALE");
    }

    [Test]
    public async Task CreateAFacilityWhoseTypeIsSentAsText()
    {
        var id = await CreateFacility(FacilityBody("Arena"));

        var (_, facility) = await GetRaw(Uris.GetFacility(id.ToString()));

        facility["type"]!.ToString().Should().Be("Arena");
    }

    [Test]
    public async Task StillAcceptAPlayerGenderSentAsInteger()
    {
        var id = await CreatePlayer(PlayerBody(1));

        var (_, player) = await GetRaw(Uris.GetPlayer(id.ToString()));

        player["gender"]!.ToString().Should().Be("FEMALE");
    }

    [Test]
    public async Task StillAcceptAFacilityTypeSentAsInteger()
    {
        var id = await CreateFacility(FacilityBody(1));

        var (_, facility) = await GetRaw(Uris.GetFacility(id.ToString()));

        facility["type"]!.ToString().Should().Be("Arena");
    }

    [Test]
    public async Task UpdateAPlayerFromARequestBoundToPublicFields()
    {
        var id = await CreatePlayer(PlayerBody("MALE"));
        var update = new
        {
            firstName = "Updated", lastName = "Name", secondLastName = "Again",
            birthDate = "1985-05-05T00:00:00", gender = "OTHER"
        };

        var response = await Send(HttpMethod.Put, Uris.UpdatePlayer(id.ToString()), update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (_, player) = await GetRaw(Uris.GetPlayer(id.ToString()));
        player["firstName"]!.ToString().Should().Be("Updated");
        player["lastName"]!.ToString().Should().Be("Name");
        player["gender"]!.ToString().Should().Be("OTHER");
    }

    private static object PlayerBody(object gender) => new
    {
        firstName = "Json", lastName = "Contract", secondLastName = (string?)null,
        birthDate = "2000-01-01T00:00:00", gender
    };

    private static object FacilityBody(object type) => new
    {
        name = "Contract Arena", active = true, buildDate = "2020-07-20T00:00:00", type, capacity = 18000,
        address = new
        {
            street = "Main Avenue", streetNumber = "1", postalCode = "08001",
            city = "Barcelona", region = "Catalonia", country = "Spain"
        }
    };

    private async Task<Guid> CreatePlayer(object body) => await Create(Uris.CreatePlayer(), body);

    private async Task<Guid> CreateFacility(object body) => await Create(Uris.CreateFacility(), body);

    private async Task<Guid> Create(Uri uri, object body)
    {
        var response = await Send(HttpMethod.Post, uri, body);
        var raw = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(raw);
        return JsonConvert.DeserializeObject<Guid>(raw);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, Uri uri, object body)
    {
        var request = new HttpRequestMessage(method, uri)
        {
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };
        return await _client.SendAsync(request);
    }

    private async Task<(string Raw, JObject Body)> GetRaw(Uri uri)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, raw);
        var body = JsonConvert.DeserializeObject<JObject>(raw,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None })!;
        return (raw, body);
    }
}
