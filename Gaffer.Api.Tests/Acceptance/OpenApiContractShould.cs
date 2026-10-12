using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;
using Newtonsoft.Json.Linq;
using NSwag;

namespace Gaffer.Api.Tests.Acceptance;

public class OpenApiContractShould {
    private StartupTestServer server;
    private JObject document;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);
        var response = await server.Client.GetAsync("/swagger/v1/swagger.json");
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, raw);
        document = JObject.Parse(raw);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await server.DisposeAsync();

    [TestCase("Gender", new[] { "MALE", "FEMALE", "OTHER" })]
    [TestCase("FacilityType", new[] { "Stadium", "Arena", "Pitch", "Training" })]
    public void DeclareEnumsAsTextWithTheirMemberNames(string schemaName, string[] memberNames) {
        var schema = Schemas()[schemaName];

        schema.Should().NotBeNull(document.ToString());
        TestContext.Out.WriteLine(schema!.ToString());
        schema["type"]!.ToString().Should().Be("string");
        schema["enum"]!.Values<string>().Should().Equal(memberNames);
    }

    [TestCase("players")]
    [TestCase("facilities")]
    public void DeclareCreatedAsTheSuccessResponseOfCreate(string resource) {
        var paths = (JObject)document["paths"]!;
        var createPath = paths.Properties().Single(path => path.Name.TrimEnd('/').EndsWith($"/{resource}"));

        var responses = (JObject)createPath.Value["post"]!["responses"]!;

        TestContext.Out.WriteLine($"{createPath.Name}: {responses}");
        responses.Properties().Select(response => response.Name).Should().Contain("201").And.NotContain("200");
    }

    [TestCase("birthDate")]
    [TestCase("gender")]
    public async Task DeclareUpdatePlayerRequestFieldAsOptionalAndNullable(string propertyName) {
        var document = await OpenApiDocument.FromJsonAsync(this.document.ToString());
        var schema = document.Definitions["UpdatePlayerRequest"];
        var rawSchema = Schemas()["UpdatePlayerRequest"]!.ToString();
        TestContext.Out.WriteLine(rawSchema);

        schema.RequiredProperties.Should().NotContain(propertyName, rawSchema);
        var property = schema.ActualProperties[propertyName];
        property.IsNullable(document.SchemaType).Should().BeTrue(rawSchema);
    }

    private JObject Schemas() => (JObject)(document["definitions"] ?? document["components"]!["schemas"])!;
}
