using ConventionSystem.Application.Convention.Queries;

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

public record EditionStaffMemberDto(
    Guid PersonId,
    string PersonName,
    string Email,
    string? Phone,
    string ApplicationStatus);

public record StaffApplicationAvailabilityDto(DateTime Start, DateTime End);

public record StaffApplicationSummaryDto(
    Guid Id,
    Guid PersonId,
    string? PersonName,
    string InterestDescription,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> StaffAreaPreferenceIds,
    IReadOnlyList<StaffApplicationAvailabilityDto> Availabilities);

public record StaffScheduleDto(
    Guid EditionId,
    Guid? StaffAreaFilterId,
    IReadOnlyList<EditionScheduleDayDto> ScheduleDays,
    IReadOnlyList<StaffScheduleAreaDto> StaffAreas);

public record StaffScheduleAreaDto(
    Guid StaffAreaId,
    string Name,
    string? Description,
    Guid ResponsibleId,
    string? ResponsibleName,
    IReadOnlyList<StaffScheduleStationDto> Stations);

public record StaffScheduleStationDto(
    Guid StationId,
    string Name,
    string? Description,
    IReadOnlyList<StaffScheduleShiftDto> Shifts);

public record StaffScheduleShiftDto(
    Guid ShiftId,
    Guid StationId,
    Guid ResponsibleId,
    string? ResponsibleName,
    DateTime Start,
    DateTime End,
    int MinPersons,
    int MaxPersons,
    int ActiveAssignmentCount,
    int ConfirmedAssignmentCount,
    string Status,
    string StaffingStatus);
