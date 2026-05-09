using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class TeamSessionAssignment
{
    public SessionId SessionId { get; private set; }
    public Guid TeamEventRegistrationId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public PersonId AssignedByPersonId { get; private set; }

    private TeamSessionAssignment() { }

    internal TeamSessionAssignment(SessionId sessionId, Guid registrationId, PersonId assignedById)
    {
        SessionId = sessionId;
        TeamEventRegistrationId = registrationId;
        AssignedAt = DateTimeOffset.UtcNow;
        AssignedByPersonId = assignedById;
    }
}
