using System.Text.Json.Serialization;

namespace ConventionSystem.Application.Export.Contracts;

public sealed record EditionExportDocument(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("durationDays")] int DurationDays,
    [property: JsonPropertyName("scheduleDays")] IReadOnlyList<ExportScheduleDayDto> ScheduleDays,
    [property: JsonPropertyName("venues")] IReadOnlyList<ExportVenueDto> Venues,
    [property: JsonPropertyName("staffAreas")] IReadOnlyList<ExportStaffAreaDto> StaffAreas,
    [property: JsonPropertyName("categories")] IReadOnlyList<ExportCategoryDto> Categories,
    [property: JsonPropertyName("events")] IReadOnlyList<ExportEventDto>? Events,
    [property: JsonPropertyName("ticketTypes")] IReadOnlyList<ExportTicketTypeDto>? TicketTypes)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ExportScheduleDayDto(
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("startTime")] string? StartTime,
    [property: JsonPropertyName("endTime")] string? EndTime);

public sealed record ExportVenueDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("building")] string Building,
    [property: JsonPropertyName("description")] string? Description);

public sealed record ExportStaffAreaDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("responsibleEmail")] string? ResponsibleEmail,
    [property: JsonPropertyName("stations")] IReadOnlyList<ExportStationDto> Stations);

public sealed record ExportStationDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("shifts")] IReadOnlyList<ExportShiftDto> Shifts);

public sealed record ExportShiftDto(
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("startTime")] string StartTime,
    [property: JsonPropertyName("endTime")] string EndTime,
    [property: JsonPropertyName("minPersons")] int MinPersons,
    [property: JsonPropertyName("maxPersons")] int MaxPersons,
    [property: JsonPropertyName("responsibleEmail")] string? ResponsibleEmail);

public sealed record ExportCategoryDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("responsibleEmail")] string? ResponsibleEmail);

public sealed record ExportEventDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("registrationType")] string RegistrationType,
    [property: JsonPropertyName("dropInRules")] string? DropInRules,
    [property: JsonPropertyName("scheduleRequestText")] string? ScheduleRequestText,
    [property: JsonPropertyName("leadOrganiserEmail")] string? LeadOrganiserEmail,
    [property: JsonPropertyName("sessions")] IReadOnlyList<ExportSessionDto> Sessions);

public sealed record ExportSessionDto(
    [property: JsonPropertyName("venueName")] string VenueName,
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("startTime")] string StartTime,
    [property: JsonPropertyName("endTime")] string EndTime,
    [property: JsonPropertyName("maxSeats")] int MaxSeats,
    [property: JsonPropertyName("startType")] string StartType);

public sealed record ExportTicketTypeDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] int Price,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("validDays")] IReadOnlyList<int>? ValidDays,
    [property: JsonPropertyName("allowedCategoryNames")] IReadOnlyList<string>? AllowedCategoryNames);
