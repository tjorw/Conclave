using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CancelOwnTicket;

public sealed record CancelOwnTicketCommand(Guid TicketId) : IRequest;
