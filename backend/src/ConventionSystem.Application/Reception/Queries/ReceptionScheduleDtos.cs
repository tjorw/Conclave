namespace ConventionSystem.Application.Reception.Queries;

public record PersonScheduleDto(
    IReadOnlyList<PersonShiftItemDto> Shifts,
    IReadOnlyList<PersonSessionItemDto> Sessions,
    IReadOnlyList<ScheduleDaySummaryDto> DailySummary,
    ScheduleTotalDto Total);

public record PersonShiftItemDto(
    Guid ShiftId,
    string AreaName,
    string StationName,
    DateOnly Date,
    DateTime Start,
    DateTime End,
    string Status,
    string Role);

public record PersonSessionItemDto(
    Guid SessionId,
    Guid EventId,
    string EventTitle,
    string Role,
    string VenueName,
    DateOnly Date,
    DateTime Start,
    DateTime End);

public record ScheduleDaySummaryDto(
    DateOnly Date,
    int ShiftCount,
    double ShiftHours,
    int SessionCount,
    double SessionHours,
    double TotalHours);

public record ScheduleTotalDto(
    double TotalShiftHours,
    double TotalSessionHours,
    double TotalHours,
    IReadOnlyList<DateOnly> WorkDays);
