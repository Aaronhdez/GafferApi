using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository
{
    Task<Player> Get(Guid newGuid);
}