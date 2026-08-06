using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class PlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    private readonly IDatabaseConnectionProvider _dbConnectionProvider = dbConnectionProvider;
    public Task<Player> Get(Guid newGuid, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}