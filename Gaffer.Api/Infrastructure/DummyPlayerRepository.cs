using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class DummyPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider, List<Player>? players)
    : IPlayerRepository
{
    private List<Player> _players = players;
    public IDatabaseConnectionProvider DbConnectionProvider { get; } = dbConnectionProvider;

    public Task<Player> Get(Guid newGuid, CancellationToken cancellationToken)
    {
        if (DatabaseIsNull()) throw new DatabaseNotReadyException($"Database not ready");
        if (DatabaseIsEmpty()) throw new PlayerNotFoundException($"Player with id {newGuid} not found");
        var player = players.FirstOrDefault(player => player.Id == newGuid);
        return player == null ? throw new PlayerNotFoundException($"Player with id {newGuid} not found") : Task.FromResult(player);
    }

    public Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (DatabaseIsNull()) throw new DatabaseNotReadyException($"Database not ready");
        if (criteria.criteriaList.Count > 0) {
            return Task.FromResult(FilteredResults(criteria));
        }
        return Task.FromResult(_players);
    }

    private List<Player> FilteredResults(PlayerSearchCriteria criteria)
    {
        var result = new List<Player>();
        if (criteria.criteriaList.Exists(s => s.Name == "Id"))
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "Id")?.Value;
            result = _players.Where(player => player.Id.ToString() == idCriteria).ToList();
        }
        if (criteria.criteriaList.Exists(s => s.Name == "FirstName"))
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "FirstName")?.Value;
            result = _players.Where(player => player.FirstName == idCriteria).ToList();
        }
        if (criteria.criteriaList.Exists(s => s.Name == "LastName"))
        {
            var idCriteria = criteria.criteriaList.FirstOrDefault(c => c.Name == "LastName")?.Value;
            result = _players.Where(player => player.LastName == idCriteria).ToList();
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