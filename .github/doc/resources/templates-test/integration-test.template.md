# Integration Test Template (NUnit + TestHost)

```csharp
using NUnit.Framework;

namespace <ModuleName>.Tests.Integration.<FeatureName>;

[TestFixture]
public class <TargetEndpointName>IntegrationTests
{
    private <TestFixtureType> _fixture = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _fixture = await <TestFixtureFactory>.CreateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _fixture.DisposeAsync();
    }

    [Test]
    public async Task <ScenarioName>_Returns_<ExpectedStatusOrPayload>()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("<uri>");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```
