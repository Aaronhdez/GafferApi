using DocumentFormat.OpenXml.Drawing.Charts;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository
{
    Task<List<Player>> Get(PlayerSearchCriteria criteria, CancellationToken cancellationToken);
    Task Save(Player player, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task Update(Player player, CancellationToken cancellationToken);
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
    OR,
    NONE
}

public class PlayerNotFoundException : Exception
{
    public PlayerNotFoundException(string message, Guid id) : base(message)
    {
        Id = id.ToString();
        ErrorMessage =  $"Player with id {id} not found in Database";
    }

    public string Id { get; set; }
    public string ErrorMessage { get; set; }
}

public class PlayerSearchFilterNotPermittedException(string fieldName)
    : Exception("FilterNotPermitted")
{
    public string FieldName { get; } = fieldName;
}