namespace ConventionSystem.Application.Staff.Queries;

public record ShiftSummaryDto(
    Guid Id,
    Guid StationId,
    Guid ResponsibleId,
    string? ResponsibleName,
    DateTime Start,
    DateTime End,
    int MinPersons,
    int MaxPersons,
    int ActiveAssignmentCount,
    string Status);

public record ShiftDto(
    Guid Id,
    Guid StationId,
    Guid ResponsibleId,
    string? ResponsibleName,
    DateTime Start,
    DateTime End,
    int MinPersons,
    int MaxPersons,
    string Status,
    IReadOnlyList<StaffAssignmentDto> Assignments);

public record StaffAssignmentDto(
    Guid Id,
    Guid PersonId,
    string? PersonName,
    string Status,
    DateTimeOffset AssignedAt);

public record StaffApplicationSummaryDto(
    Guid Id,
    Guid PersonId,
    string? PersonName,
    string InterestDescription,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> StationPreferenceIds);
