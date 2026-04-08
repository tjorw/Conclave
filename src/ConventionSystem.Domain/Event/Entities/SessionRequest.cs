using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class SessionRequest : Entity<SessionRequestId>
{
    public string Description { get; private set; } = string.Empty;
    public int RequestedDurationMinutes { get; private set; }
    public int RequestedSeats { get; private set; }
    public StartType StartType { get; private set; }

    private SessionRequest() { }

    internal SessionRequest(SessionRequestId id, string description, int durationMinutes, int seats, StartType startType)
        : base(id)
    {
        Description = description;
        RequestedDurationMinutes = durationMinutes;
        RequestedSeats = seats;
        StartType = startType;
    }
}
