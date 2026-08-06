using Gaffer.Api.Controllers;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public class SearchPlayersQueryHandler(IPlayerRepository playerRepository) : IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>
{
    public Task<List<SearchPlayerResult>> Handle(SearchPlayersQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class SearchPlayerResult
{
    public Guid Id;
    public string FirstName;
    public string LastName;
    public string? SecondLastName;
    public DateTime BirthDate;
    public Gender Gender;
}

public class SearchPlayersQuery : IQuery<List<SearchPlayerResult>>
{
}