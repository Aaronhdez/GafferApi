using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Microsoft.AspNetCore.Mvc;
using Namotion.Reflection;

namespace Gaffer.Api.Controllers;

[Route("api/v{version:apiVersion}/players")]
[ApiController]
public class PlayerController(
    IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> getPlayerByIdQueryHandler,
    ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> createPlayerCommandHandler) : ControllerBase
{
    private readonly IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> _getPlayerByIdQueryHandler = getPlayerByIdQueryHandler;

    [HttpGet("{id}")]
    public async Task<GetPlayerResponse> GetBy([FromQuery] Guid id)
    {
        var player = await _getPlayerByIdQueryHandler.Handle(new GetPlayerByIdQuery(id), CancellationToken.None);
        return new GetPlayerResponse(player.Id, player.FirstName, player.LastName, player.SecondLastName, player.Birthdate, player.Gender);
    }

    [HttpPost("")]
    public async Task<int> Create([FromBody] CreatePlayerRequest request)
    {
        throw new NotImplementedException();
    }
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