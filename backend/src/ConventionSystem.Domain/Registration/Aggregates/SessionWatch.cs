using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class SessionWatch : AggregateRoot
{
    public SessionWatchId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public SessionId SessionId { get; private set; }
    public EditionId EditionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SessionWatch() { }

    public SessionWatch(SessionWatchId id, PersonId personId, SessionId sessionId, EditionId editionId)
    {
        Id = id;
        PersonId = personId;
        SessionId = sessionId;
        EditionId = editionId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
