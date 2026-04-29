using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Domain.Staff.Events;

public record PersonAssignedToShift(
    StaffAssignmentId AssignmentId,
    ShiftId ShiftId,
    PersonId PersonId,
    PersonId AssignedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentConfirmed(
    StaffAssignmentId AssignmentId,
    ShiftId ShiftId,
    PersonId PersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentRejected(
    StaffAssignmentId AssignmentId,
    ShiftId ShiftId,
    PersonId PersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentCancelled(
    StaffAssignmentId AssignmentId,
    ShiftId ShiftId,
    PersonId PersonId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
