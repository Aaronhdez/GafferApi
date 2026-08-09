using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public class UpdatePlayerCommandHandler(IPlayerRepository playerRepository) : ICommandHandler<UpdatePlayerCommand, UpdatePlayerResult>
{
    public Task<UpdatePlayerResult> Handle(UpdatePlayerCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class UpdatePlayerCommand : ICommand<UpdatePlayerResult>
{
    public Guid Id { get; set; }
}

public class UpdatePlayerResult
{
}

