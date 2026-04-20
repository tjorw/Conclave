using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
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

        var context = await ShiftContextLoader.LoadWithAssignmentsAsync(
            shiftRepository,
            editionRepository,
            conventionRepository,
            shiftId,
            ct);

        ApplicationAuthorization.EnsureShiftManager(
            context.Convention,
            context.Edition,
            context.Shift.StationId,
            performedById,
            "Utföraren har inte behörighet att bekräfta denna tilldelning.");

        context.Shift.ConfirmAssignment(assignmentId);
        await shiftRepository.SaveAsync(ct);
    }
}
