using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class PlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    private readonly IDatabaseConnectionProvider _dbConnectionProvider = dbConnectionProvider;

    public Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}