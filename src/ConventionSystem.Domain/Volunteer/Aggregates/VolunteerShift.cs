using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Volunteer.Entities;
using ConventionSystem.Domain.Volunteer.Enums;
using ConventionSystem.Domain.Volunteer.Events;
using ConventionSystem.Domain.Volunteer.Ids;
using ConventionSystem.Domain.Volunteer.ValueObjects;

namespace ConventionSystem.Domain.Volunteer.Aggregates;

public sealed class VolunteerShift : AggregateRoot
{
    private readonly List<VolunteerAssignment> _assignments = [];

    public VolunteerShiftId Id { get; private set; }
    public StationId StationId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public StaffingRequirement StaffingRequirement { get; private set; } = null!;
    public VolunteerShiftStatus Status { get; private set; }

    public IReadOnlyList<VolunteerAssignment> Assignments => _assignments.AsReadOnly();

    private VolunteerShift() { }

    public VolunteerShift(VolunteerShiftId id, StationId stationId, TimeSlot timeSlot, StaffingRequirement staffingRequirement)
    {
        Id = id;
        StationId = stationId;
        TimeSlot = timeSlot;
        StaffingRequirement = staffingRequirement;
        Status = VolunteerShiftStatus.Planned;
    }

    public VolunteerAssignment AssignPerson(PersonId personId, PersonId assignedById)
    {
        if (Status is not (VolunteerShiftStatus.Planned or VolunteerShiftStatus.InProgress))
            throw new InvalidOperationException("Kan bara tilldela volontärer till planerade eller pågående pass.");

        if (StaffingRequirement.IsFullyStaffed(ActiveAssignmentCount()))
            throw new InvalidOperationException("Passet har redan nått maximal bemanning.");

        if (_assignments.Any(a => a.PersonId == personId &&
            a.Status is not (VolunteerAssignmentStatus.Cancelled or VolunteerAssignmentStatus.Rejected)))
            throw new InvalidOperationException("Personen är redan aktiv tilldelad detta pass.");

        var assignment = new VolunteerAssignment(VolunteerAssignmentId.New(), personId, assignedById);
        _assignments.Add(assignment);
        return assignment;
    }

    public void ConfirmAssignment(VolunteerAssignmentId assignmentId)
    {
        var assignment = GetAssignment(assignmentId);
        assignment.Confirm();
        RaiseDomainEvent(new AssignmentConfirmed(assignmentId, Id, assignment.PersonId, DateTimeOffset.UtcNow));
    }

    public void RejectAssignment(VolunteerAssignmentId assignmentId)
    {
        var assignment = GetAssignment(assignmentId);
        assignment.Reject();
        RaiseDomainEvent(new AssignmentRejected(assignmentId, Id, assignment.PersonId, DateTimeOffset.UtcNow));
    }

    public void CancelAssignment(VolunteerAssignmentId assignmentId, PersonId performedById)
    {
        var assignment = GetAssignment(assignmentId);
        assignment.Cancel();
        RaiseDomainEvent(new AssignmentCancelled(assignmentId, Id, assignment.PersonId, performedById, DateTimeOffset.UtcNow));
    }

    public void Cancel(PersonId performedById)
    {
        if (Status == VolunteerShiftStatus.Cancelled)
            throw new InvalidOperationException("Passet är redan inställt.");

        Status = VolunteerShiftStatus.Cancelled;
        RaiseDomainEvent(new VolunteerShiftCancelled(Id, StationId, performedById, DateTimeOffset.UtcNow));
    }

    private VolunteerAssignment GetAssignment(VolunteerAssignmentId assignmentId) =>
        _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException("Tilldelningen hittades inte.");

    private int ActiveAssignmentCount() => _assignments.Count(a =>
        a.Status is not (VolunteerAssignmentStatus.Cancelled or VolunteerAssignmentStatus.Rejected));
}
