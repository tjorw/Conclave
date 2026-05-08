using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class TeamEventRegistration : AggregateRoot
{
    public TeamEventRegistrationId Id { get; private set; }
    public TeamId TeamId { get; private set; }
    public EventId EventId { get; private set; }
    public EditionId EditionId { get; private set; }
    public TeamRegistrationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private TeamEventRegistration() { }

    public TeamEventRegistration(TeamEventRegistrationId id, TeamId teamId, EventId eventId, EditionId editionId)
    {
        Id = id;
        TeamId = teamId;
        EventId = eventId;
        EditionId = editionId;
        Status = TeamRegistrationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TeamEventRegistrationCreated(id, teamId, eventId, CreatedAt));
    }

    public void Confirm()
    {
        if (Status != TeamRegistrationStatus.Pending)
            throw new TeamRegistrationNotPendingException();

        Status = TeamRegistrationStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TeamEventRegistrationConfirmed(Id, TeamId, EventId, UpdatedAt.Value));
    }

    public void Cancel(PersonId cancelledByPersonId)
    {
        if (Status == TeamRegistrationStatus.Cancelled)
            throw new TeamRegistrationAlreadyCancelledException();

        Status = TeamRegistrationStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TeamEventRegistrationCancelled(Id, TeamId, EventId, cancelledByPersonId, UpdatedAt.Value));
    }
}
