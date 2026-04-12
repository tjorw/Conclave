using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class Session : Entity<SessionId>
{
    public EventId EventId { get; private set; }
    public VenueId VenueId { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public int MaxSeats { get; private set; }
    public StartType StartType { get; private set; }
    public SessionStatus Status { get; private set; }

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
            throw new InvalidOperationException("Kan inte redigera en inaktiv session.");
        VenueId = venueId;
        TimeSlot = timeSlot;
        MaxSeats = maxSeats;
        StartType = startType;
    }

    internal void Deactivate()
    {
        if (Status == SessionStatus.Inactive)
            throw new InvalidOperationException("Sessionen är redan inaktiv.");
        Status = SessionStatus.Inactive;
    }
}
