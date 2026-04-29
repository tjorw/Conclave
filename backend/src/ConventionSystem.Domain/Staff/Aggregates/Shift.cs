using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Entities;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Exceptions;
using ConventionSystem.Domain.Staff.Events;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;

namespace ConventionSystem.Domain.Staff.Aggregates;

public sealed class Shift : AggregateRoot
{
    private readonly List<StaffAssignment> _assignments = [];

    public ShiftId Id { get; private set; }
    public StationId StationId { get; private set; }
    public PersonId ResponsibleId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public StaffingRequirement StaffingRequirement { get; private set; } = null!;

    public IReadOnlyList<StaffAssignment> Assignments => _assignments.AsReadOnly();

    private Shift() { }

    public Shift(ShiftId id, StationId stationId, PersonId responsibleId, TimeSlot timeSlot, StaffingRequirement staffingRequirement)
    {
        Id = id;
        StationId = stationId;
        ResponsibleId = responsibleId;
        TimeSlot = timeSlot;
        StaffingRequirement = staffingRequirement;
    }

    public StaffAssignment AssignPerson(PersonId personId, PersonId assignedById)
    {
        if (StaffingRequirement.IsFullyStaffed(ActiveAssignmentCount()))
            throw new ShiftAlreadyFullyStaffedException();

        if (_assignments.Any(a => a.PersonId == personId &&
            a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected)))
            throw new PersonAlreadyAssignedToShiftException();

        var assignment = new StaffAssignment(StaffAssignmentId.New(), personId, assignedById);
        _assignments.Add(assignment);
        RaiseDomainEvent(new PersonAssignedToShift(assignment.Id, Id, personId, assignedById, DateTimeOffset.UtcNow));
        return assignment;
    }

    public void ConfirmAssignment(StaffAssignmentId assignmentId)
    {
        var assignment = GetAssignment(assignmentId);
        assignment.Confirm();
        RaiseDomainEvent(new AssignmentConfirmed(assignmentId, Id, assignment.PersonId, DateTimeOffset.UtcNow));
    }

    public void RejectAssignment(StaffAssignmentId assignmentId)
    {
        var assignment = GetAssignment(assignmentId);
        assignment.Reject();
        RaiseDomainEvent(new AssignmentRejected(assignmentId, Id, assignment.PersonId, DateTimeOffset.UtcNow));
    }

    public void CancelAssignment(StaffAssignmentId assignmentId, PersonId performedById)
    {
        var assignment = GetAssignment(assignmentId);
        _assignments.Remove(assignment);
        RaiseDomainEvent(new AssignmentCancelled(assignmentId, Id, assignment.PersonId, performedById, DateTimeOffset.UtcNow));
    }

    public void Update(StationId stationId, PersonId responsibleId, TimeSlot timeSlot, StaffingRequirement staffingRequirement)
    {
        StationId = stationId;
        ResponsibleId = responsibleId;
        TimeSlot = timeSlot;
        StaffingRequirement = staffingRequirement;
    }

    private StaffAssignment GetAssignment(StaffAssignmentId assignmentId) =>
        _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new StaffAssignmentNotFoundException();

    private int ActiveAssignmentCount() => _assignments.Count(a =>
        a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected));
}
