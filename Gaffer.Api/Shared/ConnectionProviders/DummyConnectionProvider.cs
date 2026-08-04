namespace Gaffer.Api.Shared.ConnectionProviders;

public class DummyConnectionProvider : IDatabaseConnectionProvider
{
    public Task<bool> TestConnection() => Task.FromResult(true);
}