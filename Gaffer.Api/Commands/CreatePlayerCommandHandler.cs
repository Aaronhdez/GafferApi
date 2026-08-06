using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public record CreatePlayerResponse(Guid Id);

public record CreatePlayerCommand(
    string FirstName,
    string LastName,
    string? SecondLastName,
    DateTime Birthdate,
    Gender Gender)
    : ICommand<CreatePlayerResponse>;

public class CreatePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>
{
    public Task<CreatePlayerResponse> Handle(CreatePlayerCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}


