using System.Globalization;
using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/players")]
[ApiController]
public class PlayerController(
    ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> createPlayerCommandHandler,
    IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> searchPlayersQueryHandler,
    ICommandHandler<DeletePlayerCommand> deletePlayerCommandHandler,
    ICommandHandler<UpdatePlayerCommand> updatePlayerCommandHandler) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<SearchPlayerResponse> GetBy([FromRoute] Guid id)
    {
        var query = new SearchPlayersQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{id}" });
        var queryResult = await searchPlayersQueryHandler.Handle(query, CancellationToken.None);
        var player = queryResult.Select(row => new SearchPlayerResponse(row.Id, row.FirstName, row.LastName, row.SecondLastName,
            row.BirthDate, row.Gender
        )).FirstOrDefault();
        return player ?? throw new PlayerNotFoundException($"Player with id {id} not found", id);
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePlayerRequest request)
    {
        var createResult = await createPlayerCommandHandler.Handle(
            new CreatePlayerCommand(request.FirstName, request.LastName, request.SecondLastName, request.BirthDate,
                request.Gender), CancellationToken.None);
        return CreatedAtAction(nameof(GetBy), new { id = createResult.Id }, createResult.Id);
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
        return queryResult.Select(row => new SearchPlayerResponse(
                row.Id,
                row.FirstName,
                row.LastName,
                row.SecondLastName,
                row.BirthDate,
                row.Gender
            ))
            .ToList();
    }

    [HttpDelete("{id}")]
    public async Task Delete([FromRoute] Guid id)
    {
        await deletePlayerCommandHandler.Handle(new DeletePlayerCommand(id),  CancellationToken.None);
    }

    [HttpPut("{id}")]
    public async Task Update([FromRoute] Guid id, [FromBody] UpdatePlayerRequest request)
    {
        await updatePlayerCommandHandler.Handle(new UpdatePlayerCommand
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            SecondLastName = request.SecondLastName,
            BirthDate = request.BirthDate.ToString(CultureInfo.InvariantCulture),
            Gender = request.Gender.ToString(),
        }, CancellationToken.None);
    }
}

public class UpdatePlayerRequest
{
    public string FirstName;
    public string LastName;
    public string? SecondLastName; 
    public DateTime BirthDate;
    public Gender Gender;
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

public record SearchPlayerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime BirthDate,
    Gender Gender);

public record CreatePlayerRequest(
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime BirthDate,
    Gender Gender);