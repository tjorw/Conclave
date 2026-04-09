using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Events;

public record ConventionCreated(
    ConventionId ConventionId,
    string Name,
    string Slug,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PersonRegistered(
    PersonId PersonId,
    ConventionId ConventionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PersonCreated(
    PersonId PersonId,
    ConventionId ConventionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PersonUpdated(
    PersonId PersonId,
    ConventionId ConventionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record PersonDeactivated(
    PersonId PersonId,
    ConventionId ConventionId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record AdministratorAdded(
    ConventionId ConventionId,
    PersonId PersonId,
    PersonId AddedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record EditionPublished(
    EditionId EditionId,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record RegistrationOpened(
    EditionId EditionId,
    RegistrationType Type,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record StructureCopiedFromEdition(
    EditionId TargetId,
    EditionId SourceId,
    int VenueCount,
    int StaffAreaCount,
    int StationCount,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
