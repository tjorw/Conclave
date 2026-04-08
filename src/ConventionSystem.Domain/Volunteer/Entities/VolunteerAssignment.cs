using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Volunteer.Enums;
using ConventionSystem.Domain.Volunteer.Ids;

namespace ConventionSystem.Domain.Volunteer.Entities;

public sealed class VolunteerAssignment : Entity<VolunteerAssignmentId>
{
    public PersonId PersonId { get; private set; }
    public PersonId AssignedById { get; private set; }
    public VolunteerAssignmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private VolunteerAssignment() { }

    internal VolunteerAssignment(VolunteerAssignmentId id, PersonId personId, PersonId assignedById)
        : base(id)
    {
        PersonId = personId;
        AssignedById = assignedById;
        Status = VolunteerAssignmentStatus.Assigned;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    internal void Confirm()
    {
        if (Status != VolunteerAssignmentStatus.Assigned)
            throw new InvalidOperationException("Tilldelningen måste vara i tilldelat läge för att bekräftas.");
        Status = VolunteerAssignmentStatus.Confirmed;
    }

    internal void Reject()
    {
        if (Status != VolunteerAssignmentStatus.Assigned)
            throw new InvalidOperationException("Tilldelningen måste vara i tilldelat läge för att avvisas.");
        Status = VolunteerAssignmentStatus.Rejected;
    }

    internal void Cancel()
    {
        if (Status == VolunteerAssignmentStatus.Cancelled)
            throw new InvalidOperationException("Tilldelningen är redan avbokad.");
        Status = VolunteerAssignmentStatus.Cancelled;
    }
}
