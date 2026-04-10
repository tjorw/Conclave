using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Events;

namespace ConventionSystem.Application.Staff.DomainEventHandlers;

public sealed class ShiftCancelledHandler(IShiftRepository shiftRepository)
    : IDomainEventHandler<ShiftCancelled>
{
    public async Task Handle(ShiftCancelled notification, CancellationToken ct)
    {
        var shift = await shiftRepository.GetByIdWithAssignmentsAsync(notification.ShiftId, ct);
        if (shift is null) return;

        var activeAssignments = shift.Assignments
            .Where(a => a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected))
            .ToList();

        foreach (var assignment in activeAssignments)
            shift.CancelAssignment(assignment.Id, notification.PerformedById);

        if (activeAssignments.Count > 0)
            await shiftRepository.SaveAsync(ct);
    }
}
