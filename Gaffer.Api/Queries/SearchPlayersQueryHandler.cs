using Gaffer.Api.Controllers;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public class SearchPlayersQueryHandler(IPlayerRepository playerRepository)
    : IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>
{
    public async Task<List<SearchPlayerResult>> Handle(SearchPlayersQuery query, CancellationToken cancellationToken)
    {
        var players = await playerRepository.Get(new PlayerSearchCriteria(), cancellationToken);
        return players.Select(player
                => new SearchPlayerResult(player.Id, player.FirstName, player.LastName, player.SecondLastName,
                    player.Birthdate, Gender.MALE))
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

public class SearchPlayersQuery : IQuery<List<SearchPlayerResult>>
{
    public List<QueryFilter> Filters { get; set; }
}

public class QueryFilter
{
    public string Name { get; set; }
    public string Value { get; set; }
}