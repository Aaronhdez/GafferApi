using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class DeletePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<DeletePlayerCommand, DeletePlayerResult>
{
    public Task<DeletePlayerResult> Handle(DeletePlayerCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public record DeletePlayerCommand(Guid Id) : ICommand<DeletePlayerResult>;

public class DeletePlayerResult
{
}