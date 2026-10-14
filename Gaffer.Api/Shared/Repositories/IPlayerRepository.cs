using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Criteria;

namespace Gaffer.Api.Shared.Repositories;

public interface IPlayerRepository {
    Task<List<Player>> Get(CriteriaSet criteria, CancellationToken cancellationToken);
    Task Save(Player player, CancellationToken cancellationToken);
    Task Delete(Guid id, CancellationToken cancellationToken);
    Task Update(Player player, CancellationToken cancellationToken);
}

public class PlayerNotFoundException(string message, Guid id) : Exception(message) {
    public string Id { get; set; } = id.ToString();
    public string ErrorMessage { get; set; } = $"Player with id {id} not found in Database";
}

public class PlayerSearchFilterNotPermittedException(string fieldName)
    : Exception("FilterNotPermitted") {
    public string FieldName { get; } = fieldName;
}
