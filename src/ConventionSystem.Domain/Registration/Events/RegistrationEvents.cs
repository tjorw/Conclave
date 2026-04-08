using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Events;

public record VisitorRegistrationConfirmed(
    VisitorRegistrationId RegistrationId,
    PersonId PersonId,
    EditionId EditionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record SessionRegistrationCancelled(
    SessionRegistrationId RegistrationId,
    SessionId SessionId,
    PersonId PersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record StaffApplicationReceived(
    StaffApplicationId ApplicationId,
    PersonId PersonId,
    EditionId EditionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TicketCollected(
    TicketId TicketId,
    PersonId PersonId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TicketRevoked(
    TicketId TicketId,
    PersonId PersonId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
