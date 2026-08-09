using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public class SearchPlayerByIdQueryHandler(IPlayerRepository playerRepository)
    : IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse>
{
    public async Task<SearchPlayerByIdResponse> Handle(SearchPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        var searchCriteria = new PlayerSearchCriteria();
        searchCriteria.criteriaList.Add(new SearchCriteria { Name = "Id", Value = $"{query.Id}", });
        var queryResult = await playerRepository.Get(searchCriteria, cancellationToken);
        var player = queryResult.FirstOrDefault();
        if (player == null) throw new PlayerNotFoundException($"Player with id {query.Id} not found");
        return new SearchPlayerByIdResponse
        {
            Id = player.Id,
            FirstName = player.FirstName,
            LastName = player.LastName,
            SecondLastName = player.SecondLastName,
            Birthdate = player.Birthdate,
            Gender = player.Gender
        };
    }
}

public class SearchPlayerByIdResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime Birthdate { get; set; }
    public Gender Gender { get; set; }
}

public record SearchPlayerByIdQuery(Guid Id) : IQuery<SearchPlayerByIdResponse>
{
    public Guid Id { get; } = Id;
}