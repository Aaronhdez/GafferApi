using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class DeletePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<DeletePlayerCommand>
{
    public async Task Handle(DeletePlayerCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var player = await FindPlayer(command, cancellationToken);
            if (player == null) throw new PlayerNotFoundException($"Player with id {command.Id} not found", command.Id);
            await playerRepository.Delete(command.Id, cancellationToken);
        }
        catch (PlayerNotFoundException ex)
        {
            throw new CannotDeletePlayerNotFoundException(ex.Message, command.Id);
        }
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

public class CannotDeletePlayerNotFoundException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
}

public class CannotDeletePlayerException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}

public record DeletePlayerCommand(Guid Id) : ICommand;