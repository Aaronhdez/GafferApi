using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;



public class GetPlayerByIdQueryHandler(IPlayerRepository playerRepository) : IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>
{
    private readonly IPlayerRepository _playerRepository = playerRepository;
    
    public async Task<GetPlayerByIdResponse> Handle(GetPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.Get(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        return new GetPlayerByIdResponse
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FirstName = string.Empty,
            LastName = string.Empty,
            SecondLastName = null,
            BirthDate = DateTime.UtcNow,
            Gender = Gender.OTHER
        };
    }
}

public class GetPlayerByIdResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime BirthDate { get; set; }
    public Gender Gender { get; set; }
}

public record GetPlayerByIdQuery(Guid Id) : IQuery<GetPlayerByIdResponse> {
    public Guid Id { get; } = Id;
}