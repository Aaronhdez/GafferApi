namespace Gaffer.Api.Shared.ConnectionProviders;

public interface IDatabaseConnectionProvider
{
    public Task<bool> TestConnection();
}