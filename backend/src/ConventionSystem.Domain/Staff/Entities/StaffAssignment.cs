using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Exceptions;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Domain.Staff.Entities;

public sealed class StaffAssignment : Entity<StaffAssignmentId>
{
    public PersonId PersonId { get; private set; }
    public PersonId AssignedById { get; private set; }
    public StaffAssignmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private StaffAssignment() { }

    internal StaffAssignment(StaffAssignmentId id, PersonId personId, PersonId assignedById)
        : base(id)
    {
        PersonId = personId;
        AssignedById = assignedById;
        Status = StaffAssignmentStatus.Assigned;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    internal void Confirm()
    {
        if (Status != StaffAssignmentStatus.Assigned)
            throw new AssignmentMustBeAssignedToConfirmException();
        Status = StaffAssignmentStatus.Confirmed;
    }

    internal void Reject()
    {
        if (Status != StaffAssignmentStatus.Assigned)
            throw new AssignmentMustBeAssignedToRejectException();
        Status = StaffAssignmentStatus.Rejected;
    }

    internal void Cancel()
    {
        if (Status == StaffAssignmentStatus.Cancelled)
            throw new AssignmentAlreadyCancelledException();
        if (Status == StaffAssignmentStatus.Rejected)
            throw new RejectedAssignmentCannotBeCancelledException();
        Status = StaffAssignmentStatus.Cancelled;
    }
}
