using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateStation;

public sealed record CreateStationCommand(
    Guid EditionId,
    string Name,
    string? Description,
    Guid StaffAreaId,
    Guid PerformedById) : IRequest<Guid>;
