using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository
{
    Task<Player> Get(Guid newGuid, CancellationToken cancellationToken);
    Task<List<Player>> Get(PlayerSearchCriteria searchCriteria, CancellationToken cancellationToken);
    Task Save(Player player, CancellationToken cancellationToken);
}

public class PlayerSearchCriteria {
    public List<SearchCriteria> SearchCriteria { get; set; }
}

public class SearchCriteria 
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class PlayerNotFoundException(string message) : Exception(message);
public class DatabaseNotReadyException(string message) : Exception(message);