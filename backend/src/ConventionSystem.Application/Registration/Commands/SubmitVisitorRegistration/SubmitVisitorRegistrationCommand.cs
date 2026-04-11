using MediatR;

namespace ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;

public sealed record SubmitVisitorRegistrationCommand(
    Guid EditionId,
    Guid PersonId,
    Guid TicketTypeId) : IRequest<Guid>;
