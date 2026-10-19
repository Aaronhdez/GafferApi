using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Criteria;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Players;

public class UpdatePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<UpdatePlayerCommand> {
    public async Task Handle(UpdatePlayerCommand command, CancellationToken cancellationToken) {
        try {
            var player = await GetPlayer(command, cancellationToken);
            UpdatePlayer(player, command);
            await playerRepository.Update(player, cancellationToken);
        } catch (PlayerNotFoundException ex) {
            throw new CannotUpdatePlayerNotFoundException(ex.Message, command.Id);
        }
    }

    private async Task<Player> GetPlayer(UpdatePlayerCommand command, CancellationToken cancellationToken) {
        var playerSearchCriteria = new CriteriaSet();
        playerSearchCriteria.ValueCriteria.Add(new ValueCriteria {
            Name = "Id",
            Values = [$"{command.Id}"],
            Type = ValueCriteriaType.AND
        });
        var queryResult = await playerRepository.Get(playerSearchCriteria, cancellationToken);
        var player = queryResult.FirstOrDefault();
        return player ?? throw new PlayerNotFoundException($"Player with id {command.Id} not found", command.Id);
    }

    private static void UpdatePlayer(Player player, UpdatePlayerCommand command) {
        var errors = new DomainValidationErrors();
        if (command.FirstName is not null) errors.Collect(() => player.SetFirstName(command.FirstName));
        if (command.LastName is not null) errors.Collect(() => player.SetLastName(command.LastName));
        if (command.SecondLastName is not null) errors.Collect(() => player.SetSecondLastName(command.SecondLastName));
        if (command.BirthDate.HasValue) errors.Collect(() => player.SetBirthDate(command.BirthDate.Value));
        if (command.Gender.HasValue) errors.Collect(() => player.SetGender(command.Gender.Value));

        try {
            errors.ThrowIfAny();
        } catch (DomainValidationException ex) {
            throw new ValidationFailedException(CannotUpdatePlayerTitle, ValidationErrorKeys.PlayersUpdate, ex.Errors);
        }
    }

    private const string CannotUpdatePlayerTitle = "Cannot update player";
}

public class CannotUpdatePlayerNotFoundException(string message, Guid id) : Exception(message) {
    public Guid Id { get; set; } = id;
}

public class CannotUpdatePlayerException(string message, Guid id) : Exception(message) {
    public Guid Id { get; set; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}

public class UpdatePlayerCommand : ICommand {
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public Gender? Gender { get; set; }
}
