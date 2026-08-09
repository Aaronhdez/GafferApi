using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class DeletePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<DeletePlayerCommand, DeletePlayerResult>
{
    public async Task<DeletePlayerResult> Handle(DeletePlayerCommand command, CancellationToken cancellationToken)
    {
        await playerRepository.Delete(command.Id, cancellationToken);
        return new DeletePlayerResult();
    }
}

public record DeletePlayerCommand(Guid Id) : ICommand<DeletePlayerResult>;

public class DeletePlayerResult
{
}