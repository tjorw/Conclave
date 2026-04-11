using MediatR;

namespace ConventionSystem.Application.Registration.Commands.IssueTicket;

public sealed record IssueTicketCommand(
    Guid PersonId,
    Guid EditionId,
    Guid TicketTypeId) : IRequest<Guid>;
