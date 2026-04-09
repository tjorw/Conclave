using MediatR;

namespace ConventionSystem.Application.Registration.Commands.IssueTicket;

public sealed record IssueTicketCommand(
    Guid PersonId,
    Guid EditionId,
    Guid TicketTypeId,
    Guid PerformedById) : IRequest<Guid>;
