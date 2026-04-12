using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Events;

public record EventCreated(
    EventId EventId,
    EditionId EditionId,
    CategoryId CategoryId,
    PersonId LeadOrganiserId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EventSubmittedForReview(
    EventId EventId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EventApproved(
    EventId EventId,
    PersonId LeadOrganiserId,
    PersonId ReviewedById,
    string EventTitle,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EventRejected(
    EventId EventId,
    PersonId LeadOrganiserId,
    PersonId ReviewedById,
    string EventTitle,
    string RejectionComment,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EventCancelled(
    EventId EventId,
    PersonId ResponsibleId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record SessionCreated(
    EventId EventId,
    SessionId SessionId,
    VenueId VenueId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record SessionDeactivated(
    SessionId SessionId,
    EventId EventId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
