using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

// Läs-modell som aggregerar engagemang från Event-, Registration- och Staff-kontexterna.
// Korskontext-join för bemanningspass: Shift har inget direkt EditionId – kopplingen
// går via stations-tabellens shadow FK (EditionId), som löses med SelectMany på
// Edition.Stations-navigeringen. Detta är ett medvetet val för read models och ska
// inte reproduceras i skrivvägar.
public sealed class MyScheduleRepository(ConventionDbContext db) : IMyScheduleRepository
{
    public async Task<IReadOnlyList<MyOrganiserSessionSummaryDto>> ListMyOrganiserSessionsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Where(e => e.EditionId == editionId
                     && e.Status == EventStatus.Published
                     && (e.LeadOrganiserId == personId || e.CoOrganisers.Any(c => c.PersonId == personId)))
            .ToListAsync(ct);

        var activeSessions = events
            .SelectMany(e => e.Sessions
                .Where(s => s.Status == SessionStatus.Active)
                .Select(s => (Event: e, Session: s)))
            .ToList();

        var venueIds = activeSessions.Select(x => x.Session.VenueId).Distinct().ToHashSet();
        var venueMap = venueIds.Count > 0
            ? await db.Set<Venue>()
                .Where(v => venueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Name, ct)
            : new Dictionary<VenueId, string>();

        return activeSessions
            .Select(x => new MyOrganiserSessionSummaryDto(
                x.Session.Id.Value,
                x.Event.Title ?? "",
                x.Session.TimeSlot.Start,
                x.Session.TimeSlot.End,
                venueMap.GetValueOrDefault(x.Session.VenueId) ?? ""))
            .OrderBy(x => x.Start)
            .ToList();
    }

    public async Task<IReadOnlyList<MyAssignedShiftSummaryDto>> ListMyAssignedShiftsAsync(
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
                     && s.Status != ShiftStatus.Cancelled
                     && (s.ResponsibleId == personId
                      || s.Assignments.Any(a => a.PersonId == personId
                          && (a.Status == StaffAssignmentStatus.Assigned
                           || a.Status == StaffAssignmentStatus.Confirmed))))
            .ToListAsync(ct);

        var usedStationIds = shifts.Select(s => s.StationId).ToHashSet();
        var stationNameMap = usedStationIds.Count > 0
            ? await db.Set<Station>()
                .Where(s => usedStationIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<StationId, string>();

        return shifts
            .Select(s => new MyAssignedShiftSummaryDto(
                s.Id.Value,
                stationNameMap.GetValueOrDefault(s.StationId) ?? "",
                s.ResponsibleId == personId ? "Responsible" : "Assigned",
                s.TimeSlot.Start,
                s.TimeSlot.End))
            .OrderBy(x => x.Start)
            .ToList();
    }
}
