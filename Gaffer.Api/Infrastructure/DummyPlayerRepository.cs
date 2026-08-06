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
        if (DatabaseIsEmpty()) throw new PlayerNotFoundException($"Player with id {newGuid} not found");
        var player = players.FirstOrDefault(player => player.Id == newGuid);
        return player == null ? throw new PlayerNotFoundException($"Player with id {newGuid} not found") : Task.FromResult(player);
    }

    public Task Save(Player player, CancellationToken cancellationToken)
    {
        _players.Add(player);
        return Task.CompletedTask;
    }

    private bool DatabaseIsEmpty() => players == null || players.Count == 0;
}