using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdateEdition;

public sealed record UpdateEditionCommand(
    Guid EditionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId) : IRequest;
