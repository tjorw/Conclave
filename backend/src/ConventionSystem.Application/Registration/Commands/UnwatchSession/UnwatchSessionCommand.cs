
namespace ConventionSystem.Application.Registration.Commands.UnwatchSession;

public sealed record UnwatchSessionCommand(Guid SessionId) : ICommand;
