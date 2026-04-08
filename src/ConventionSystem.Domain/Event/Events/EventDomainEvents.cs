using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Events;

public record VersionApproved(
    EventId EventId,
    EventVersionId VersionId,
    PersonId ResponsibleId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record VersionRejected(
    EventId EventId,
    EventVersionId VersionId,
    PersonId ResponsibleId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EventCancelled(
    EventId EventId,
    PersonId ResponsibleId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record SessionDeactivated(
    SessionId SessionId,
    EventId EventId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
