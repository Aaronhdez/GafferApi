using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class SqlPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider) : IPlayerRepository
{
    public async Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = "SELECT * FROM Players";
        var result =  await dbConnectionProvider.QueryAsync<Player>(query, cancellationToken);
        return result.ToList();
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}