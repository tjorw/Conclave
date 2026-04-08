using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Entities;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Events;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;

namespace ConventionSystem.Domain.Staff.Aggregates;

public sealed class Shift : AggregateRoot
{
    private readonly List<StaffAssignment> _assignments = [];

    public ShiftId Id { get; private set; }
    public StationId StationId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public StaffingRequirement StaffingRequirement { get; private set; } = null!;
    public ShiftStatus Status { get; private set; }

    public IReadOnlyList<StaffAssignment> Assignments => _assignments.AsReadOnly();

    private Shift() { }

    public Shift(ShiftId id, StationId stationId, TimeSlot timeSlot, StaffingRequirement staffingRequirement)
    {
        Id = id;
        StationId = stationId;
        TimeSlot = timeSlot;
        StaffingRequirement = staffingRequirement;
        Status = ShiftStatus.Planned;
    }

    public StaffAssignment AssignPerson(PersonId personId, PersonId assignedById)
    {
        if (Status is not (ShiftStatus.Planned or ShiftStatus.InProgress))
            throw new InvalidOperationException("Kan bara tilldela personal till planerade eller pågående pass.");

        if (StaffingRequirement.IsFullyStaffed(ActiveAssignmentCount()))
            throw new InvalidOperationException("Passet har redan nått maximal bemanning.");

        if (_assignments.Any(a => a.PersonId == personId &&
            a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected)))
            throw new InvalidOperationException("Personen är redan aktiv tilldelad detta pass.");

        var assignment = new StaffAssignment(StaffAssignmentId.New(), personId, assignedById);
        _assignments.Add(assignment);
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
        assignment.Cancel();
        RaiseDomainEvent(new AssignmentCancelled(assignmentId, Id, assignment.PersonId, performedById, DateTimeOffset.UtcNow));
    }

    public void Cancel(PersonId performedById)
    {
        if (Status == ShiftStatus.Cancelled)
            throw new InvalidOperationException("Passet är redan inställt.");

        Status = ShiftStatus.Cancelled;
        RaiseDomainEvent(new ShiftCancelled(Id, StationId, performedById, DateTimeOffset.UtcNow));
    }

    private StaffAssignment GetAssignment(StaffAssignmentId assignmentId) =>
        _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException("Tilldelningen hittades inte.");

    private int ActiveAssignmentCount() => _assignments.Count(a =>
        a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected));
}
