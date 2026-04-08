using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Convention.Events;

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
    int StationCount,
    PersonId PerformedById,
    DateTimeOffset OccurredAt) : IDomainEvent;
