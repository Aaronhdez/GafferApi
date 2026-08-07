using Gaffer.Api.Domain;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class DummyPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider, List<Player>? players)
    : IPlayerRepository
{
    private List<Player> _players = players;
    public IDatabaseConnectionProvider DbConnectionProvider { get; } = dbConnectionProvider;

    public Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (DatabaseIsNull()) throw new DatabaseNotReadyException($"Database not ready");
        return Task.FromResult(criteria.criteriaList.Count > 0 
            ? FilteredResults(criteria)
            : _players);
    }

    private List<Player> FilteredResults(PlayerSearchCriteria criteria)
    {
        var result = new List<Player>();
        result.AddRange(_players);
        if (criteria.criteriaList.Exists(s => s.Name == "Id"))
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "Id")?.Value;
            result = result.Where(player => player.PlayerId.ToString() == idCriteria).ToList();
        }
        if (criteria.criteriaList.Exists(s => s.Name == "FirstName")) 
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "FirstName")?.Value;
            result = result.Where(player => player.FirstName == idCriteria).ToList();
        } 
        if (criteria.criteriaList.Exists(s => s.Name == "LastName"))
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "LastName")?.Value;
            result = result.Where(player => player.LastName == idCriteria).ToList();
        }
        return result;
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        if(DatabaseIsNull()) throw new DatabaseNotReadyException($"Database not ready");
        _players.Add(player);
        return Task.CompletedTask;
    }

    private bool DatabaseIsNull() => players == null;
    private bool DatabaseIsEmpty() => players.Count == 0;
}