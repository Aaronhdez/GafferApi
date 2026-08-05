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
}

public record GetPlayerByIdQuery(Guid Id) : IQuery<GetPlayerByIdResponse> {
    public Guid Id { get; } = Id;
}