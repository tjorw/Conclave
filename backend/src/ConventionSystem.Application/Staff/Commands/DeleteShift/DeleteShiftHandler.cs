using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Commands.DeleteShift;

public sealed class DeleteShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<DeleteShiftCommand>
{
    protected override async Task ExecuteAsync(DeleteShiftCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var performedById = currentUser.PersonId;

        var context = await ShiftContextLoader.LoadWithAssignmentsAsync(
            shiftRepository,
            editionRepository,
            conventionRepository,
            shiftId,
            ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan ta bort pass.");

        var shift = context.Shift;
        var activeAssignmentIds = shift.Assignments
            .Where(a => a.Status is StaffAssignmentStatus.Assigned or StaffAssignmentStatus.Confirmed)
            .Select(a => a.Id)
            .ToList();

        foreach (var assignmentId in activeAssignmentIds)
            shift.CancelAssignment(assignmentId, performedById);

        await shiftRepository.DeleteAndSaveAsync(shift, ct);
    }
}
