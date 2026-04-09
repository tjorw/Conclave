using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateEdition;

public sealed record CreateEditionCommand(
    Guid ConventionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    Guid PerformedById) : IRequest<Guid>;
