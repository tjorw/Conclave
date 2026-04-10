namespace ConventionSystem.Application.Staff.Queries;

public record ShiftSummaryDto(
    Guid Id,
    Guid StationId,
    Guid ResponsibleId,
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
    DateTime Start,
    DateTime End,
    int MinPersons,
    int MaxPersons,
    string Status,
    IReadOnlyList<StaffAssignmentDto> Assignments);

public record StaffAssignmentDto(
    Guid Id,
    Guid PersonId,
    Guid AssignedById,
    string Status,
    DateTimeOffset AssignedAt);
