using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CollectTicket;

public sealed record CollectTicketCommand(
    Guid TicketId) : IRequest<CollectTicketResult>;

public sealed record CollectTicketResult(
    Guid TicketId,
    IReadOnlyList<string> Perks);
