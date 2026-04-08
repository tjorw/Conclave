using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Volunteer.Ids;

namespace ConventionSystem.Domain.Volunteer.Events;

public record VolunteerShiftCancelled(
    VolunteerShiftId ShiftId,
    StationId StationId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentConfirmed(
    VolunteerAssignmentId AssignmentId,
    VolunteerShiftId ShiftId,
    PersonId PersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentRejected(
    VolunteerAssignmentId AssignmentId,
    VolunteerShiftId ShiftId,
    PersonId PersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AssignmentCancelled(
    VolunteerAssignmentId AssignmentId,
    VolunteerShiftId ShiftId,
    PersonId PersonId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
