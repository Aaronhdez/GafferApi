using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository
{
    Task<Player> Get(Guid newGuid, CancellationToken cancellationToken);
    Task Save(Player player, CancellationToken cancellationToken);
}

public class PlayerNotFoundException(string message) : Exception(message);