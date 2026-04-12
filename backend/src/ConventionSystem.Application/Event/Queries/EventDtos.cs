namespace ConventionSystem.Application.Event.Queries;

public record EventSummaryDto(
    Guid Id,
    Guid EditionId,
    Guid CategoryId,
    string? CategoryName,
    Guid LeadOrganiserId,
    string? LeadOrganiserName,
    string Status,
    string? Title,
    int SessionCount,
    string Description,
    IReadOnlyList<SessionSummaryDto> Sessions);

public record SessionSummaryDto(
    Guid Id,
    Guid VenueId,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    string StartType,
    string Status);

public record EventDto(
    Guid Id,
    Guid EditionId,
    Guid CategoryId,
    Guid LeadOrganiserId,
    string? LeadOrganiserName,
    string Status,
    string Title,
    string Description,
    string RegistrationType,
    string? DropInRules,
    IReadOnlyList<Guid> CoOrganiserIds,
    IReadOnlyList<SessionRequestDto> SessionRequests,
    IReadOnlyList<SessionDto> Sessions,
    IReadOnlyList<EventCommentDto> Comments);

public record SessionRequestDto(
    Guid Id,
    string Description,
    int DurationMinutes,
    int Seats,
    string StartType);

public record SessionDto(
    Guid Id,
    Guid VenueId,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    string StartType,
    string Status);

public record EventCommentDto(
    Guid Id,
    Guid AuthorId,
    string Text,
    DateTimeOffset CreatedAt);
