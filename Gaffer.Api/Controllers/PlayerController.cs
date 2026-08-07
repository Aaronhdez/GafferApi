using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/players")]
[ApiController]
public class PlayerController(
    IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> getPlayerByIdQueryHandler,
    ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> createPlayerCommandHandler,
    IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> searchPlayersQueryHandler) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<GetPlayerResponse> GetBy([FromQuery] Guid id)
    {
        var query = new SearchPlayersQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{id}" });
        var queryResult = await searchPlayersQueryHandler.Handle(query, CancellationToken.None);
        return queryResult.Select(row => new GetPlayerResponse(row.Id, row.FirstName, row.LastName, row.SecondLastName,
            row.BirthDate, row.Gender
        )).First();
    }

    [HttpPost("")]
    public async Task<Guid> Create([FromBody] CreatePlayerRequest request)
    {
        var createResult = await createPlayerCommandHandler.Handle(
            new CreatePlayerCommand(request.FirstName, request.LastName, request.SecondLastName, request.BirthDate,
                request.Gender), CancellationToken.None);
        return createResult.Id;
    }

    [HttpPost("search")]
    public async Task<List<SearchPlayerResponse>> Search(SearchPlayersRequest request)
    {
        var query = new SearchPlayersQuery();
        foreach (var filter in request.Filters)
        {
            query.Filters.Add(new QueryFilter { Name = filter.Name, Value = filter.Value });
        }

        var queryResult = await searchPlayersQueryHandler.Handle(query, CancellationToken.None);
        return queryResult.Select(row => new SearchPlayerResponse
            {
                Id = row.Id,
                FirstName = row.FirstName,
                LastName = row.LastName,
                SecondLastName = row.SecondLastName,
                BirthDate = row.BirthDate,
                Gender = row.Gender
            })
            .ToList();
    }
}

public class SearchPlayersRequest
{
    public List<SearchPlayersFilter> Filters { get; set; } = [];
}

public class SearchPlayersFilter
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class SearchPlayerResponse
{
    public Guid Id;
    public string FirstName;
    public string LastName;
    public string? SecondLastName;
    public DateTime BirthDate;
    public Gender Gender;
}

public record CreatePlayerRequest(
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime BirthDate,
    Gender Gender);

public record GetPlayerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime BirthDate,
    Gender Gender);