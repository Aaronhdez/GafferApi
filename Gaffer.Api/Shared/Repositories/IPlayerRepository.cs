using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository
{
    Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken);
    Task Save(Player player, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
}

public class PlayerSearchCriteria
{
    public List<SearchCriteria> criteriaList { get; set; } = [];
}

public class SearchCriteria 
{
    public string Name { get; set; }
    public string Value { get; set; }
    public CriteriaType CriteriaType { get; set; } = CriteriaType.AND;
}

public enum CriteriaType
{
    AND,
    OR
}

public class PlayerNotFoundException(string message) : Exception(message);
public class DatabaseNotReadyException(string message) : Exception(message);