using ConventionSystem.Application.Reception.Abstractions;
using ConventionSystem.Application.Reception.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

// Läs-modell för receptionsvy: aggregerar arbetspass och arrangörstider från
// Staff- och Event-kontexterna för en given person och upplaga.
// Korskontext-join för bemanningspass: se kommentar i MyScheduleRepository.
public sealed class ReceptionScheduleRepository(ConventionDbContext db) : IReceptionScheduleRepository
{
    public async Task<IReadOnlyList<PersonShiftItemDto>> ListShiftsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var stationIds = await db.Editions
            .Where(e => e.Id == editionId)
            .SelectMany(e => e.Stations)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (stationIds.Count == 0)
            return [];

        var shifts = await db.Shifts
            .Include(s => s.Assignments)
            .Where(s => stationIds.Contains(s.StationId)
                     && (s.ResponsibleId == personId
                      || s.Assignments.Any(a => a.PersonId == personId
                          && (a.Status == StaffAssignmentStatus.Assigned
                           || a.Status == StaffAssignmentStatus.Confirmed))))
            .OrderBy(s => s.TimeSlot.Start)
            .ToListAsync(ct);

        if (shifts.Count == 0)
            return [];

        var usedStationIds = shifts.Select(s => s.StationId).ToHashSet();
        var stations = await db.Stations
            .Where(s => usedStationIds.Contains(s.Id))
            .ToListAsync(ct);

        var areaIds = stations.Select(s => s.StaffAreaId).ToHashSet();
        var areaNames = await db.StaffAreas
            .Where(a => areaIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, ct);

        var stationMap = stations.ToDictionary(
            s => s.Id,
            s => new { s.Name, s.StaffAreaId });

        return shifts.Select(shift =>
        {
            var station = stationMap.GetValueOrDefault(shift.StationId);
            var areaName = station is not null
                ? areaNames.GetValueOrDefault(station.StaffAreaId) ?? ""
                : "";
            var assignmentStatus = shift.Assignments
                .FirstOrDefault(a => a.PersonId == personId
                    && (a.Status == StaffAssignmentStatus.Assigned
                     || a.Status == StaffAssignmentStatus.Confirmed))
                ?.Status.ToString() ?? string.Empty;
            var role = shift.ResponsibleId == personId ? "Responsible" : "Assigned";

            return new PersonShiftItemDto(
                shift.Id.Value,
                areaName,
                station?.Name ?? "",
                DateOnly.FromDateTime(shift.TimeSlot.Start),
                shift.TimeSlot.Start,
                shift.TimeSlot.End,
                assignmentStatus,
                role);
        }).ToList();
    }

    public async Task<IReadOnlyList<PersonSessionItemDto>> ListOrganiserSessionsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Where(e => e.EditionId == editionId
                     && e.Status != EventStatus.Cancelled
                     && (e.LeadOrganiserId == personId || e.CoOrganisers.Any(c => c.PersonId == personId)))
            .ToListAsync(ct);

        var activeSessions = events
            .SelectMany(e => e.Sessions
                .Where(s => s.Status == SessionStatus.Active)
                .Select(s => (Event: e, Session: s)))
            .ToList();

        if (activeSessions.Count == 0)
            return [];

        var venueIds = activeSessions.Select(x => x.Session.VenueId).Distinct().ToHashSet();
        var venueNames = await db.Venues
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, ct);

        return activeSessions
            .Select(x => new PersonSessionItemDto(
                x.Session.Id.Value,
                x.Event.Id.Value,
                x.Event.Title ?? "",
                x.Event.LeadOrganiserId == personId ? "Huvudarrangör" : "Medarrangör",
                venueNames.GetValueOrDefault(x.Session.VenueId) ?? "",
                DateOnly.FromDateTime(x.Session.TimeSlot.Start),
                x.Session.TimeSlot.Start,
                x.Session.TimeSlot.End))
            .OrderBy(x => x.Start)
            .ToList();
    }
}
