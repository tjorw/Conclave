using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class Session : Entity<SessionId>
{
    private readonly List<TeamSessionAssignment> _teamAssignments = [];

    public EventId EventId { get; private set; }
    public VenueId VenueId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public int MaxSeats { get; private set; }
    public StartType StartType { get; private set; }
    public SessionStatus Status { get; private set; }

    public IReadOnlyList<TeamSessionAssignment> TeamAssignments => _teamAssignments.AsReadOnly();

    private Session() { }

    internal Session(SessionId id, EventId eventId, VenueId venueId, TimeSlot timeSlot, int maxSeats, StartType startType)
        : base(id)
    {
        EventId = eventId;
        VenueId = venueId;
        TimeSlot = timeSlot;
        MaxSeats = maxSeats;
        StartType = startType;
        Status = SessionStatus.Active;
    }

    internal void Update(VenueId venueId, TimeSlot timeSlot, int maxSeats, StartType startType)
    {
        if (Status == SessionStatus.Inactive)
            throw new SessionInactiveCannotEditException();
        VenueId = venueId;
        TimeSlot = timeSlot;
        MaxSeats = maxSeats;
        StartType = startType;
    }

    internal void Deactivate()
    {
        if (Status == SessionStatus.Inactive)
            throw new SessionAlreadyInactiveException();
        Status = SessionStatus.Inactive;
    }

    internal TeamSessionAssignment AssignTeam(Guid registrationId, PersonId assignedById)
    {
        if (Status == SessionStatus.Inactive)
            throw new SessionInactiveCannotEditException();
        if (_teamAssignments.Any(a => a.TeamEventRegistrationId == registrationId))
            throw new TeamAlreadyAssignedToSessionException();

        var assignment = new TeamSessionAssignment(Id, registrationId, assignedById);
        _teamAssignments.Add(assignment);
        return assignment;
    }

    internal void RemoveTeamAssignment(Guid registrationId)
    {
        var assignment = _teamAssignments.FirstOrDefault(a => a.TeamEventRegistrationId == registrationId)
            ?? throw new TeamAssignmentNotFoundException();
        _teamAssignments.Remove(assignment);
    }
}
