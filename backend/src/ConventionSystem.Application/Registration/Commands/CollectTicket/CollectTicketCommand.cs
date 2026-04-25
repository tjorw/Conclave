
namespace ConventionSystem.Application.Registration.Commands.CollectTicket;

public sealed record CollectTicketCommand(
    Guid TicketId) : ICommand<CollectTicketResult>;

public sealed record CollectTicketResult(
    Guid TicketId,
    string? Description);
