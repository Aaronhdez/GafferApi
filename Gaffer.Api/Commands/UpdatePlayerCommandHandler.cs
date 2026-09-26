using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class UpdatePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<UpdatePlayerCommand>
{
    public async Task Handle(UpdatePlayerCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var player = await GetPlayer(command, cancellationToken);
            UpdatePlayer(player, command);
            await playerRepository.Update(player, cancellationToken);
        }
        catch (PlayerNotFoundException ex)
        {
            throw new CannotUpdatePlayerNotFoundException(ex.Message, command.Id);
        }
    }

    private async Task<Player> GetPlayer(UpdatePlayerCommand command, CancellationToken cancellationToken)
    {
        var playerSearchCriteria = new PlayerSearchCriteria();
        playerSearchCriteria.criteriaList.Add(new SearchCriteria
        {
            Name = "Id",
            Value = $"{command.Id}",
            CriteriaType = CriteriaType.NONE
        });
        var queryResult = await playerRepository.Get(playerSearchCriteria, cancellationToken);
        var player = queryResult.FirstOrDefault();
        return player ?? throw new PlayerNotFoundException($"Player with id {command.Id} not found", command.Id);
    }

    private static void UpdatePlayer(Player player, UpdatePlayerCommand command)
    {
        if (!string.IsNullOrEmpty(command.FirstName))
        {
            player.SetFirstName(command.FirstName);
        }

        if (!string.IsNullOrEmpty(command.LastName))
        {
            player.SetLastName(command.LastName);
        }

        // SecondLastName is always replaced: absent, null or empty clears it.
        player.SetSecondLastName(string.IsNullOrEmpty(command.SecondLastName) ? null : command.SecondLastName);

        if (command.BirthDate.HasValue)
        {
            player.SetBirthDateName(command.BirthDate.Value);
        }

        if (command.Gender.HasValue)
        {
            player.SetGender(command.Gender.Value);
        }
    }
}

public class CannotUpdatePlayerNotFoundException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; set; } = id;
}

public class CannotUpdatePlayerException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; set; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}

public class UpdatePlayerCommand : ICommand
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SecondLastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public Gender? Gender { get; set; }
}