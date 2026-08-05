using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;



public class GetPlayerByIdQueryHandler(IPlayerRepository playerRepository) : IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>
{
    private readonly IPlayerRepository _playerRepository = playerRepository;
    
    public async Task<GetPlayerByIdResponse> Handle(GetPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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