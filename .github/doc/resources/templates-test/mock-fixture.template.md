# Mock Fixture Template

```csharp
using NSubstitute;

namespace <ModuleName>.Tests.Fixtures;

public static class <TargetName>MockFixture
{
    public static I<DependencyType> BuildDefault()
    {
        var mock = Substitute.For<I<DependencyType>>();
        mock.<MethodA>(Arg.Any<<InputA>>(), Arg.Any<CancellationToken>())
            .Returns(<DefaultOutputA>);
        return mock;
    }

    public static I<DependencyType> BuildFailing(Exception ex)
    {
        var mock = Substitute.For<I<DependencyType>>();
        mock.<MethodA>(Arg.Any<<InputA>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw ex);
        return mock;
    }
}
```
