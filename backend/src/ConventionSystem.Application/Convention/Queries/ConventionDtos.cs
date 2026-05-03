namespace ConventionSystem.Application.Convention.Queries;

public record ConventionDto(Guid Id, string Name, string Slug, Guid? ActiveEditionId);

public record EditionSummaryDto(Guid Id, string Name, DateOnly Start, DateOnly End, string Status);

public record EditionScheduleDayDto(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime);

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
    IReadOnlyList<EditionScheduleDayDto> ScheduleDays,
    IReadOnlyList<VenueDto> Venues,
    IReadOnlyList<StaffAreaDto> StaffAreas,
    IReadOnlyList<StationDto> Stations,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<ProgramTagDefinitionDto> ProgramTagDefinitions);

public record PersonDto(Guid Id, string Name, string Email, string? Phone, bool IsActive, bool IsAdmin, bool HasAccount, bool IsLocked);

public record VenueDto(Guid Id, string Name, string Building, string? Description);

public record StaffAreaDto(Guid Id, string Name, string? Description, Guid ResponsibleId);

public record StationDto(Guid Id, Guid StaffAreaId, string Name, string? Description);

public record CategoryDto(Guid Id, string Name, string? OrganizerInstructions, string? PublicDescription, Guid ResponsibleId);

public record ProgramTagDefinitionDto(string Name);

public record EditionResponsibleDto(string Position, Guid? PersonId, string? PersonName, string? Email);

public record PersonSearchResultDto(
    Guid PersonId,
    string Name,
    string Email,
    string? Phone,
    IReadOnlyList<TicketSummaryForReceptionDto> Tickets);

public record TicketSummaryForReceptionDto(Guid TicketId, string TicketTypeName, string Status);
