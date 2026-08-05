using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public class GetPlayerByIdQueryHandler(IPlayerRepository playerRepository)
    : IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>
{
    private readonly IPlayerRepository _playerRepository = playerRepository;

    public async Task<GetPlayerByIdResponse> Handle(GetPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.Get(query.Id);
        return new GetPlayerByIdResponse
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

public class GetPlayerByIdResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime Birthdate { get; set; }
    public Gender Gender { get; set; }
}

public record GetPlayerByIdQuery(Guid Id) : IQuery<GetPlayerByIdResponse>
{
    public Guid Id { get; } = Id;
}