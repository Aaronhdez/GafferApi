using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Exceptions;
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
    public async Task<CreatePlayerResponse> Handle(CreatePlayerCommand command, CancellationToken cancellationToken)
    {
        var newPlayer = CreatePlayer(command);
        await playerRepository.Save(newPlayer, cancellationToken);
        return new CreatePlayerResponse(newPlayer.Id);
    }

    private static Player CreatePlayer(CreatePlayerCommand command)
    {
        try
        {
            return Player.Create(Guid.NewGuid(), command.FirstName, command.LastName, command.SecondLastName,
                command.Gender, command.Birthdate);
        }
        catch (DomainValidationException ex)
        {
            throw new ValidationFailedException(CannotCreatePlayerTitle, ValidationErrorKeys.PlayersCreate, ex.Errors);
        }
    }

    private const string CannotCreatePlayerTitle = "Cannot create player";
}


