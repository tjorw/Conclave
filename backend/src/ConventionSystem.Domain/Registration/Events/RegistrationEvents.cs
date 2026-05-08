using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Enums;

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

public record StaffApplicationAccepted(
    StaffApplicationId ApplicationId,
    PersonId PersonId,
    EditionId EditionId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record StaffApplicationRejected(
    StaffApplicationId ApplicationId,
    PersonId PersonId,
    EditionId EditionId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TicketPaid(
    TicketId TicketId,
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

public record PromotionCodeCreated(
    PromotionCodeId PromotionCodeId,
    EditionId EditionId,
    string Code,
    PersonId CreatedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PromotionCodeRedeemed(
    PromotionCodeId PromotionCodeId,
    TicketId TicketId,
    PersonId PersonId,
    int DiscountApplied,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PromotionCodeDeactivated(
    PromotionCodeId PromotionCodeId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TeamCreated(
    TeamId TeamId,
    EditionId EditionId,
    PersonId CaptainPersonId,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TeamEventRegistrationCreated(
    TeamEventRegistrationId RegistrationId,
    TeamId TeamId,
    EventId EventId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TeamEventRegistrationConfirmed(
    TeamEventRegistrationId RegistrationId,
    TeamId TeamId,
    EventId EventId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TeamEventRegistrationCancelled(
    TeamEventRegistrationId RegistrationId,
    TeamId TeamId,
    EventId EventId,
    PersonId CancelledByPersonId,
    DateTimeOffset OccurredAt) : IDomainEvent;
