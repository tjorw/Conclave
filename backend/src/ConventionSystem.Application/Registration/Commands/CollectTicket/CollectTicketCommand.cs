using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CollectTicket;

public sealed record CollectTicketCommand(
    Guid TicketId) : IRequest;
