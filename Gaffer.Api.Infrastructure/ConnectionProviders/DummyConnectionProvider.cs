namespace Gaffer.Api.Infrastructure.Databases;

public class DummyConnectionProvider : IDatabaseConnectionProvider
{
    public Task<bool> TestConnection() => Task.FromResult(true);
}