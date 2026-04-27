using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Commands.RejectAssignment;

public sealed class RejectAssignmentHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RejectAssignmentCommand>
{
    protected override async Task ExecuteAsync(RejectAssignmentCommand command, CancellationToken ct)
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

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan avslå personalplaceringar.");
        context.Shift.RejectAssignment(assignmentId);
        await shiftRepository.SaveAsync(ct);
    }
}
