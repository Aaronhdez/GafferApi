using Gaffer.Api.Controllers;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Criteria;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries.Players;

public class SearchPlayersQueryHandler(IPlayerRepository playerRepository)
    : IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> {
    public virtual async Task<List<SearchPlayerResult>> Handle(SearchPlayersQuery query, CancellationToken cancellationToken) {
        var searchCriteria = new CriteriaSet();
        foreach (var queryFilter in query.Filters) {
            searchCriteria.ValueCriteria.Add(new ValueCriteria {
                Name = queryFilter.Name,
                Values = [queryFilter.Value],
                Type = ValueCriteriaType.AND
            });
        }
        var players = await playerRepository.Get(searchCriteria, cancellationToken);
        return players.Select(player
                => new SearchPlayerResult(player.Id, player.FirstName, player.LastName, player.SecondLastName,
                    player.Birthdate, player.Gender))
            .ToList();
    }
}

public record SearchPlayerResult(
    Guid Id,
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime BirthDate,
    Gender Gender);

public class SearchPlayersQuery : IQuery<List<SearchPlayerResult>> {
    public List<QueryFilter> Filters { get; } = [];
}
