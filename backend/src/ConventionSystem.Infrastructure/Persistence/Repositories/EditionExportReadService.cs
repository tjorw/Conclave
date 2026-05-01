using System.Globalization;
using ConventionSystem.Application.Export.Abstractions;
using ConventionSystem.Application.Export.Contracts;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Staff.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EditionExportReadService(ConventionDbContext db) : IEditionExportReadService
{
    public async Task<EditionExportDocument?> BuildDocumentAsync(
        Guid editionId,
        bool includeEvents,
        bool includeTicketTypes,
        CancellationToken ct = default)
    {
        var id = new EditionId(editionId);
        var edition = await db.Editions
            .AsNoTracking()
            .Include(e => e.ScheduleDays)
            .Include(e => e.Venues)
            .Include(e => e.StaffAreas)
            .Include(e => e.Stations)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (edition is null)
            return null;

        var events = includeEvents
            ? await db.Events
                .AsNoTracking()
                .Include(e => e.Sessions)
                .Where(e => e.EditionId == id && e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.Title)
                .ToListAsync(ct)
            : [];

        var stationIds = edition.Stations.Select(s => s.Id).ToHashSet();
        var shifts = stationIds.Count == 0
            ? []
            : await db.Shifts
                .AsNoTracking()
                .Where(s => stationIds.Contains(s.StationId))
                .OrderBy(s => s.TimeSlot.Start)
                .ThenBy(s => s.TimeSlot.End)
                .ToListAsync(ct);

        var shiftsByStation = shifts
            .GroupBy(s => s.StationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var personIds = edition.StaffAreas.Select(a => a.ResponsibleId)
            .Concat(edition.Categories.Select(c => c.ResponsibleId))
            .Concat(events.Select(e => e.LeadOrganiserId))
            .Concat(shifts.Select(s => s.ResponsibleId))
            .Distinct()
            .ToHashSet();

        var personEmails = await db.Persons
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Email, ct);

        var categoriesById = edition.Categories.ToDictionary(c => c.Id.Value, c => c.Name);
        var venuesById = edition.Venues.ToDictionary(v => v.Id, v => v.Name);
        var startDate = edition.Period.StartDate;

        var ticketTypeEntities = includeTicketTypes
            ? await db.TicketTypes
                .AsNoTracking()
                .Where(t => t.EditionId == id)
                .OrderBy(t => t.Name)
                .ToListAsync(ct)
            : null;

        var ticketTypes = ticketTypeEntities?
            .Select(t => new ExportTicketTypeDto(
                t.Name,
                t.Price,
                t.Type.ToString(),
                t.Description,
                t.ValidDays == null
                    ? null
                    : t.ValidDays
                        .OrderBy(d => d)
                        .Select(d => ToRelativeDay(startDate, d))
                        .ToList(),
                t.AllowedCategories == null
                    ? null
                    : t.AllowedCategories
                        .Select(categoryId => categoriesById.GetValueOrDefault(categoryId))
                        .Where(name => name != null)
                        .Select(name => name!)
                        .OrderBy(name => name)
                        .ToList()))
            .ToList();

        return new EditionExportDocument(
            EditionExportDocument.CurrentSchemaVersion,
            edition.Name,
            edition.Period.DurationDays(),
            edition.ScheduleDays
                .OrderBy(d => d.Date)
                .Select(d => new ExportScheduleDayDto(
                    ToRelativeDay(startDate, d.Date),
                    FormatTime(d.StartTime),
                    FormatTime(d.EndTime)))
                .ToList(),
            edition.Venues
                .OrderBy(v => v.Name)
                .Select(v => new ExportVenueDto(v.Name, v.Building, v.Description))
                .ToList(),
            edition.StaffAreas
                .OrderBy(a => a.Name)
                .Select(a => new ExportStaffAreaDto(
                    a.Name,
                    a.Description,
                    personEmails.GetValueOrDefault(a.ResponsibleId),
                    edition.Stations
                        .Where(s => s.StaffAreaId == a.Id)
                        .OrderBy(s => s.Name)
                        .Select(s => new ExportStationDto(
                            s.Name,
                            s.Description,
                            shiftsByStation.GetValueOrDefault(s.Id, [])
                                .Select(shift => new ExportShiftDto(
                                    ToRelativeDay(startDate, DateOnly.FromDateTime(shift.TimeSlot.Start)),
                                    FormatTime(shift.TimeSlot.Start),
                                    FormatTime(shift.TimeSlot.End),
                                    shift.StaffingRequirement.MinPersons,
                                    shift.StaffingRequirement.MaxPersons,
                                    personEmails.GetValueOrDefault(shift.ResponsibleId)))
                                .ToList()))
                        .ToList()))
                .ToList(),
            edition.Categories
                .OrderBy(c => c.Name)
                .Select(c => new ExportCategoryDto(
                    c.Name,
                    c.OrganizerInstructions,
                    c.PublicDescription,
                    c.PublicDescription,
                    personEmails.GetValueOrDefault(c.ResponsibleId)))
                .ToList(),
            includeEvents
                ? events
                    .Select(e => new ExportEventDto(
                        e.Title,
                        e.Description,
                        categoriesById.GetValueOrDefault(e.CategoryId.Value) ?? "",
                        e.RegistrationType.ToString(),
                        e.DropInRules,
                        e.ScheduleRequestText,
                        e.CoOrganiserLimit,
                        personEmails.GetValueOrDefault(e.LeadOrganiserId),
                        e.Sessions
                            .Where(s => s.Status == SessionStatus.Active)
                            .OrderBy(s => s.TimeSlot.Start)
                            .Select(s => new ExportSessionDto(
                                venuesById.GetValueOrDefault(s.VenueId) ?? "",
                                ToRelativeDay(startDate, DateOnly.FromDateTime(s.TimeSlot.Start)),
                                FormatTime(s.TimeSlot.Start),
                                FormatTime(s.TimeSlot.End),
                                s.MaxSeats,
                                s.StartType.ToString()))
                            .ToList()))
                    .ToList()
                : null,
            ticketTypes);
    }

    private static int ToRelativeDay(DateOnly startDate, DateOnly date)
        => date.DayNumber - startDate.DayNumber + 1;

    private static string? FormatTime(TimeOnly? time)
        => time?.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime time)
        => TimeOnly.FromDateTime(time).ToString("HH:mm", CultureInfo.InvariantCulture);
}
