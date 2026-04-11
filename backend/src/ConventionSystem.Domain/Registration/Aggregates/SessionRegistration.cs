using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class SessionRegistration : AggregateRoot
{
    public SessionRegistrationId Id { get; private set; }
    public SessionId SessionId { get; private set; }
    public PersonId PersonId { get; private set; }
    public TicketId TicketId { get; private set; }
    public SessionRegistrationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SessionRegistration() { }

    public SessionRegistration(SessionRegistrationId id, SessionId sessionId, PersonId personId, TicketId ticketId)
    {
        Id = id;
        SessionId = sessionId;
        PersonId = personId;
        TicketId = ticketId;
        Status = SessionRegistrationStatus.Confirmed;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SessionRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Sessionsregistreringen är redan avbokad.");

        Status = SessionRegistrationStatus.Cancelled;
        RaiseDomainEvent(new SessionRegistrationCancelled(Id, SessionId, PersonId, DateTimeOffset.UtcNow));
    }
}
