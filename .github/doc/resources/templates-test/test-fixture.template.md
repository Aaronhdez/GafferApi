# Test Fixture Template

```csharp
namespace <ModuleName>.Tests.Fixtures;

public sealed class <FeatureFixture> : IAsyncDisposable
{
    public HttpClient Client { get; }

    private <FeatureFixture>(HttpClient client)
    {
        Client = client;
    }

    public static async Task<<FeatureFixture>> CreateAsync()
    {
        var client = <HostFactory>.CreateClient();
        var fixture = new <FeatureFixture>(client);
        await fixture.ResetStateAsync();
        return fixture;
    }

    public Task GivenAsync(<GivenStateType> state)
    {
        // Seed test data and preconditions.
        return Task.CompletedTask;
    }

    public Task<<ResultType>> WhenAsync(<InputType> input)
    {
        // Execute endpoint or use case.
        throw new NotImplementedException();
    }

    public Task ResetStateAsync()
    {
        // Clear or reseed deterministic environment.
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
```
