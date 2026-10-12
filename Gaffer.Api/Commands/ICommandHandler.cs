namespace Gaffer.Api.Commands;

public interface ICommand<out TResult> { }
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult> {
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommand { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand {
    Task Handle(TCommand command, CancellationToken cancellationToken);
}
