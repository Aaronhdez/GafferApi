namespace Gaffer.Api.Infrastructure.Databases;

public interface IDatabaseConnectionProvider
{
    public Task<bool> TestConnection();
}