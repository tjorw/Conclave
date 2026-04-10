namespace ConventionSystem.Application.Event.Queries;

public record EventSummaryDto(
    Guid Id,
    Guid EditionId,
    Guid CategoryId,
    string? CategoryName,
    Guid LeadOrganiserId,
    string Status,
    string? Title,
    int SessionCount);

public record EventDto(
    Guid Id,
    Guid EditionId,
    Guid CategoryId,
    Guid LeadOrganiserId,
    string Status,
    IReadOnlyList<Guid> CoOrganiserIds,
    EventVersionDto? PublishedVersion,
    EventVersionDto? DraftVersion,
    IReadOnlyList<SessionDto> Sessions);

public record EventVersionDto(
    Guid Id,
    string Title,
    string Description,
    string RegistrationType,
    string? DropInRules,
    string Status);

public record SessionDto(
    Guid Id,
    Guid VenueId,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    string StartType,
    string Status);
