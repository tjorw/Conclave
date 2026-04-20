
namespace ConventionSystem.Application.Registration.Commands.RegisterForSession;

public sealed record RegisterForSessionCommand(
    Guid SessionId,
    Guid TicketId) : ICommand<Guid>;
