# Unit Test Template (NUnit)

```csharp
using NUnit.Framework;
using NSubstitute;

namespace <ModuleName>.Tests.Unit.<FeatureName>;

[TestFixture]
public class <TargetClassName>Tests
{
    private I<DependencyA> _dependencyA = null!;
    private I<DependencyB> _dependencyB = null!;
    private <TargetClassName> _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dependencyA = Substitute.For<I<DependencyA>>();
        _dependencyB = Substitute.For<I<DependencyB>>();
        _sut = new <TargetClassName>(_dependencyA, _dependencyB);
    }

    [Test]
    public async Task <MethodName>_When_<Condition>_Returns_<ExpectedOutcome>()
    {
        // Arrange
        var input = <Factory>.CreateValidInput();
        _dependencyA.<SomeCall>(Arg.Any<<InputType>>(), Arg.Any<CancellationToken>())
            .Returns(<ExpectedValue>);

        // Act
        var result = await _sut.<MethodName>(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        await _dependencyA.Received(1).<SomeCall>(Arg.Any<<InputType>>(), Arg.Any<CancellationToken>());
    }
}
```
