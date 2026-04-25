namespace ConventionSystem.Application.Event.Queries;

public record EditionOrganiserDto(
    Guid PersonId,
    string PersonName,
    string Email,
    string? Phone,
    Guid EventId,
    string EventTitle,
    string Role);

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
    int PendingCommentCount,
    int PendingCoOrganiserApplicationCount,
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
    string? CategoryName,
    Guid? CategoryResponsibleId,
    string? CategoryResponsibleName,
    Guid LeadOrganiserId,
    string? LeadOrganiserName,
    string Status,
    string Title,
    string Description,
    string? ScheduleRequestText,
    string RegistrationType,
    string? DropInRules,
    IReadOnlyList<Guid> CoOrganiserIds,
    IReadOnlyList<CoOrganiserDto> CoOrganisers,
    IReadOnlyList<CoOrganiserApplicationDto> CoOrganiserApplications,
    IReadOnlyList<SessionDto> Sessions,
    IReadOnlyList<EventCommentDto> Comments);

public record CoOrganiserDto(
    Guid PersonId,
    string? PersonName);

public record CoOrganiserApplicationDto(
    Guid Id,
    string Email,
    string? Name,
    string? Message,
    string Status,
    Guid RequestedById,
    DateTimeOffset RequestedAt,
    Guid? ReviewedById,
    DateTimeOffset? ReviewedAt,
    string? ReviewComment,
    Guid? ApprovedPersonId);

public record SessionDto(
    Guid Id,
    Guid VenueId,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    string StartType,
    string Status);

public record EditionSessionDto(
    Guid SessionId,
    Guid EventId,
    string EventTitle,
    Guid VenueId,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    string StartType,
    string Status);

public record EventCommentDto(
    Guid Id,
    Guid AuthorId,
    string? AuthorName,
    string Text,
    string Status,
    bool RequiresHandling,
    string? HandlingComment,
    Guid? HandledById,
    string? HandledByName,
    DateTimeOffset? HandledAt,
    Guid? AcknowledgedById,
    DateTimeOffset? AcknowledgedAt,
    DateTimeOffset CreatedAt);
