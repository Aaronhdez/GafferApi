using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Queries;

public class GetPlayerByIdQueryHandler(IPlayerRepository playerRepository)
    : IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>
{
    private readonly IPlayerRepository _playerRepository = playerRepository;

    public async Task<GetPlayerByIdResponse> Handle(GetPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.Id == Guid.Parse("00000000-0000-0000-0000-000000000001"))
        {
            var player = await _playerRepository.Get(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            return new GetPlayerByIdResponse
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                FirstName = string.Empty,
                LastName = string.Empty,
                SecondLastName = null,
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.OTHER
            };
        }
        
        if (query.Id == Guid.Parse("00000000-0000-0000-0000-000000000002"))
        {
            var player = await _playerRepository.Get(Guid.Parse("00000000-0000-0000-0000-000000000002"));
            return new GetPlayerByIdResponse
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                FirstName = "John",
                LastName = "String",
                SecondLastName = "Doe",
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Gender.OTHER
            };
        }

        return null;
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

public record GetPlayerByIdQuery(Guid Id) : IQuery<GetPlayerByIdResponse>
{
    public Guid Id { get; } = Id;
}