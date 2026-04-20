namespace ConventionSystem.Application.Common;

public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand
{
    public async Task<Unit> Handle(TCommand command, CancellationToken ct)
    {
        await ExecuteAsync(command, ct);
        return Unit.Value;
    }

    protected abstract Task ExecuteAsync(TCommand command, CancellationToken ct);
}
