namespace ConventionSystem.Application.Feed;

public record EditionFeedDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool OrganiserRegistrationOpen,
    bool StaffRegistrationOpen,
    bool VisitorRegistrationOpen,
    IReadOnlyList<VenueFeedDto> Venues,
    IReadOnlyList<CategoryFeedDto> Categories,
    IReadOnlyList<EventSummaryFeedDto> Events);

public record VenueFeedDto(Guid Id, string Name, string Building, string? Description);

public record CategoryFeedDto(Guid Id, string Name, string? PublicDescription);

public record EventSummaryFeedDto(
    Guid Id,
    Guid CategoryId,
    string? CategoryName,
    string Title,
    string Description,
    IReadOnlyList<string> ProgramTags,
    string? LeadOrganiserName,
    int SessionCount,
    IReadOnlyList<SessionSummaryFeedDto> Sessions);

public record SessionSummaryFeedDto(
    Guid Id,
    string VenueName,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    int BookedSeats,
    string StartType);

public record EventFeedDto(
    Guid Id,
    Guid EditionId,
    Guid CategoryId,
    string? CategoryName,
    string Title,
    string Description,
    IReadOnlyList<string> ProgramTags,
    string RegistrationType,
    string? DropInRules,
    IReadOnlyList<SessionFeedDto> Sessions,
    string RegistrationMode,
    int? MinTeamSize,
    int? MaxTeamSize);

public record SessionFeedDto(
    Guid Id,
    Guid VenueId,
    string VenueName,
    DateTime Start,
    DateTime End,
    int MaxSeats,
    int BookedSeats,
    string StartType);
