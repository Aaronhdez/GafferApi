using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance;

/// <summary>
/// Permanent helper (not a check): writes the <c>swagger.json</c> served by the production pipeline
/// (<see cref="StartupTestServer"/>) byte for byte to the file named by <see cref="OutputPathVariable"/>, so that
/// two captures (before/after a change) can be compared with <c>cmp</c> or sha256. It is <c>[Explicit]</c>: the
/// normal suite never runs it. Usage (Git Bash):
/// <code>
/// GAFFER_SWAGGER_DUMP_PATH=/path/swagger.json dotnet test Gaffer.Api.Tests --filter "FullyQualifiedName~ServedOpenApiDocumentDump"
/// </code>
/// </summary>
[Explicit("Helper that dumps the served swagger.json; run it on demand with GAFFER_SWAGGER_DUMP_PATH set.")]
public class ServedOpenApiDocumentDump
{
    public const string OutputPathVariable = "GAFFER_SWAGGER_DUMP_PATH";

    [Test]
    public async Task WriteTheServedSwaggerJsonToTheConfiguredPath()
    {
        var outputPath = Environment.GetEnvironmentVariable(OutputPathVariable);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Assert.Fail($"Set the environment variable {OutputPathVariable} to the file where the served swagger.json must be written. Nothing was written.");
        }

        await using var server = await StartupTestServer.StartAsync(TestDefaultValues.TestConnectionString);
        var response = await server.Client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsByteArrayAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fullPath = Path.GetFullPath(outputPath!);
        await File.WriteAllBytesAsync(fullPath, content);
        TestContext.Out.WriteLine($"swagger.json: {content.Length} bytes written to {fullPath}");
    }
}
