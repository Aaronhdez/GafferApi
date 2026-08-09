using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class UpdatePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<UpdatePlayerCommand>
{
    public async Task Handle(UpdatePlayerCommand command, CancellationToken cancellationToken)
    {
        var player = await GetPlayer(command, cancellationToken);
        UpdatePlayer(player, command);
        playerRepository.Update(player, cancellationToken);
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
        return player ?? throw new PlayerNotFoundException($"Player with id {command.Id} not found");
    }

    private void UpdatePlayer(Player player, UpdatePlayerCommand command)
    {
        if (player.GetFirstName() != command.FirstName && !string.IsNullOrEmpty(command.FirstName)) {
            player.SetFirstName(command.FirstName);
        }
        if (player.GetLastName() != command.LastName && !string.IsNullOrEmpty(command.LastName)) {
            player.SetLastName(command.LastName);
        }
        if (player.GetSecondLastName() != command.SecondLastName && !string.IsNullOrEmpty(command.SecondLastName)) {
            player.SetSecondLastName(command.SecondLastName);
        }
        if (player.GetBirthDateName() != command.BirthDate && !string.IsNullOrEmpty(command.BirthDate)) {
            player.SetBirthDateName(command.BirthDate);
        }
        if (player.GetGenderName() != command.Gender && !string.IsNullOrEmpty(command.Gender)) {
            player.SetGender(command.Gender);
        }
    }
}

public class UpdatePlayerCommand : ICommand<UpdatePlayerResult>, ICommand
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string SecondLastName { get; set; }
    public string BirthDate { get; set; }
    public string Gender { get; set; }
}

public class UpdatePlayerResult
{
}

