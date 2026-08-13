# Acceptance Test Template (Feature-Level)

```csharp
using NUnit.Framework;

namespace <ModuleName>.Tests.Acceptance.<FeatureName>;

[TestFixture]
public class <FeatureName>AcceptanceTests
{
    private <FeatureFixture> _fixture = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = await <FeatureFixture>.CreateAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _fixture.DisposeAsync();
    }

    [Test]
    public async Task <BusinessScenario>_Should_<ExpectedBehavior>()
    {
        // Arrange
        await _fixture.GivenAsync(<GivenState>);

        // Act
        var result = await _fixture.WhenAsync(<ActionInput>);

        // Assert
        result.Should().BeEquivalentTo(<ExpectedResult>);
    }
}
```
