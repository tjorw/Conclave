using MediatR;

namespace ConventionSystem.Application.Registration.Commands.DeleteTicketType;

public sealed record DeleteTicketTypeCommand(Guid TicketTypeId) : IRequest;
