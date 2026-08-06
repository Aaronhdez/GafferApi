using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;
using Namotion.Reflection;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/players")]
[ApiController]
public class PlayerController(
    IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> getPlayerByIdQueryHandler,
    ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> createPlayerCommandHandler,
    IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> searchPlayersQueryHandler) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<GetPlayerResponse> GetBy([FromQuery] Guid id)
    {
        var queryResult = await getPlayerByIdQueryHandler.Handle(new GetPlayerByIdQuery(id), CancellationToken.None);
        return new GetPlayerResponse(queryResult.Id, queryResult.FirstName, queryResult.LastName,
            queryResult.SecondLastName,
            queryResult.Birthdate, queryResult.Gender);
    }

    [HttpPost("")]
    public async Task<Guid> Create([FromBody] CreatePlayerRequest request)
    {
        var createResult = await createPlayerCommandHandler.Handle(
            new CreatePlayerCommand(request.FirstName, request.LastName, request.SecondLastName, request.BirthDate,
                request.Gender), CancellationToken.None);
        return createResult.Id;
    }

    public async Task<List<SearchPlayerResponse>> Search(SearchPlayersRequest request)
    {
        throw new NotImplementedException();
    }
}

public class SearchPlayersRequest
{
}

public class SearchPlayerResponse
{
    public Guid Id;
    public string FirstName;
    public string LastName;
    public string? SecondLastName;
    public DateTime BirthDate;
    public Gender Gende;
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