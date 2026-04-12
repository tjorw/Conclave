using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdateStaffArea;

public sealed record UpdateStaffAreaCommand(
    Guid EditionId,
    Guid StaffAreaId,
    string Name,
    string? Description,
    Guid ResponsibleId) : IRequest;
