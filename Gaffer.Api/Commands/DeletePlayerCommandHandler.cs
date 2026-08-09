using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class DeletePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<DeletePlayerCommand, DeletePlayerResult>
{
    public async Task<DeletePlayerResult> Handle(DeletePlayerCommand command, CancellationToken cancellationToken)
    {
        var player = await FindPlayer(command, cancellationToken);
        if (player == null) throw new PlayerNotFoundException($"Player with id {command.Id} not found");
        await playerRepository.Delete(command.Id, cancellationToken);
        return new DeletePlayerResult();
    }

    private async Task<Player?> FindPlayer(DeletePlayerCommand command, CancellationToken cancellationToken)
    {
        var criteria = new SearchCriteria
        {
            Name = "Id",
            Value = command.Id.ToString(),
            CriteriaType = CriteriaType.NONE
        };
        var searchCriteria = new PlayerSearchCriteria();
        searchCriteria.criteriaList.Add(criteria);
        var queryResult = await playerRepository.Get(searchCriteria, cancellationToken);
        var player = queryResult.FirstOrDefault();
        return player;
    }
}

public record DeletePlayerCommand(Guid Id) : ICommand<DeletePlayerResult>;

public class DeletePlayerResult
{
}