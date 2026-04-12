namespace ConventionSystem.Application.Convention.Queries;

public record ConventionDto(Guid Id, string Name, string Slug);

public record EditionSummaryDto(Guid Id, string Name, DateOnly Start, DateOnly End, string Status);

public record EditionDto(
    Guid Id,
    Guid ConventionId,
    string Name,
    DateOnly Start,
    DateOnly End,
    string Status,
    bool OrganiserRegistrationOpen,
    bool StaffRegistrationOpen,
    bool VisitorRegistrationOpen,
    Guid? StaffCoordinatorId,
    Guid? EventCoordinatorId,
    IReadOnlyList<VenueDto> Venues,
    IReadOnlyList<StaffAreaDto> StaffAreas,
    IReadOnlyList<StationDto> Stations,
    IReadOnlyList<CategoryDto> Categories);

public record PersonDto(Guid Id, string Name, string Email, string? Phone, bool IsActive, bool IsAdmin);

public record VenueDto(Guid Id, string Name, string Building, string? Description);

public record StaffAreaDto(Guid Id, string Name, string? Description, Guid ResponsibleId);

public record StationDto(Guid Id, Guid StaffAreaId, string Name, string? Description);

public record CategoryDto(Guid Id, string Name, string? Description, Guid ResponsibleId);
