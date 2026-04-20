
namespace ConventionSystem.Application.Registration.Commands.WatchSession;

public sealed record WatchSessionCommand(Guid SessionId) : ICommand;
