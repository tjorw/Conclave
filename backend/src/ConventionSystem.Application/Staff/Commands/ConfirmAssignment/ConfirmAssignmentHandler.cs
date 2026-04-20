using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Commands.ConfirmAssignment;

public sealed class ConfirmAssignmentHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<ConfirmAssignmentCommand>
{
    protected override async Task ExecuteAsync(ConfirmAssignmentCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var assignmentId = new StaffAssignmentId(command.AssignmentId);
        var performedById = currentUser.PersonId;

        var shift = await shiftRepository.GetByIdWithAssignmentsAsync(shiftId, ct)
            ?? throw new InvalidOperationException($"Pass '{command.ShiftId}' hittades inte.");

        var edition = await editionRepository.GetByStationIdAsync(shift.StationId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(shift.StationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att bekräfta denna tilldelning.");

        shift.ConfirmAssignment(assignmentId);
        await shiftRepository.SaveAsync(ct);
    }
}
