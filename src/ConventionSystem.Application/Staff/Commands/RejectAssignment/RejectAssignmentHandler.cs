using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Ids;
using MediatR;

namespace ConventionSystem.Application.Staff.Commands.RejectAssignment;

public sealed class RejectAssignmentHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<RejectAssignmentCommand>
{
    public async Task Handle(RejectAssignmentCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var assignmentId = new StaffAssignmentId(command.AssignmentId);
        var performedById = new PersonId(command.PerformedById);

        var shift = await shiftRepository.GetByIdWithAssignmentsAsync(shiftId, ct)
            ?? throw new InvalidOperationException($"Pass '{command.ShiftId}' hittades inte.");

        var edition = await editionRepository.GetByStationIdAsync(shift.StationId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(shift.StationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att avvisa denna tilldelning.");

        shift.RejectAssignment(assignmentId);
        await shiftRepository.SaveAsync(ct);
    }
}
