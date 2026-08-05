using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Infrastructure;

public class DummyPlayerRepository(IDatabaseConnectionProvider dbConnectionProvider, List<Player> players)
    : IPlayerRepository
{
    private List<Player> _players = players;
    public IDatabaseConnectionProvider DbConnectionProvider { get; } = dbConnectionProvider;

    public Task<Player> Get(Guid newGuid)
    {
        if (players == null) throw new PlayerNotFoundException($"Player with id {newGuid} not found");
        if (players.Count == 0) throw new PlayerNotFoundException($"Player with id {newGuid} not found");
        var player = players.FirstOrDefault(player => player.Id == newGuid);
        if (player == null) throw new PlayerNotFoundException($"Player with id {newGuid} not found");
        return Task.FromResult(player);
    }
}