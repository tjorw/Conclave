using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
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
    public async Task<IReadOnlyList<MyScheduleItemDto>> GetMyScheduleAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        // Gemensam bas: alla publicerade evenemang med aktiva sessioner för upplagan.
        // Inkluderar co-organisatörer för att slippa en extra runda mot databasen.
        var events = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Where(e => e.EditionId == editionId && e.Status == EventStatus.Published)
            .ToListAsync(ct);

        var activeSessions = events
            .SelectMany(e => e.Sessions
                .Where(s => s.Status == SessionStatus.Active)
                .Select(s => (Event: e, Session: s)))
            .ToList();

        var activeSessionDict = activeSessions.ToDictionary(x => x.Session.Id);

        var venueIds = activeSessions.Select(x => x.Session.VenueId).Distinct().ToHashSet();
        var venueMap = venueIds.Count > 0
            ? await db.Set<Venue>()
                .Where(v => venueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Name, ct)
            : new Dictionary<VenueId, string>();

        // 1. Bokade sessioner
        var registrations = await db.SessionRegistrations
            .Where(r => r.PersonId == personId && r.Status != SessionRegistrationStatus.Cancelled)
            .ToListAsync(ct);

        var bookedSessionIds = new HashSet<SessionId>();
        var booked = registrations
            .Where(r => activeSessionDict.ContainsKey(r.SessionId))
            .Select(r =>
            {
                bookedSessionIds.Add(r.SessionId);
                var (evt, s) = activeSessionDict[r.SessionId];
                return new MyScheduleItemDto(
                    s.Id.Value, null,
                    evt.Title ?? "",
                    s.TimeSlot.Start, s.TimeSlot.End,
                    venueMap.GetValueOrDefault(s.VenueId),
                    "Booked", true);
            })
            .ToList();

        // 2. Arrangörssessioner (hoppa över redan bokade)
        var organiserSessionIds = new HashSet<SessionId>();
        var organiser = activeSessions
            .Where(x => !bookedSessionIds.Contains(x.Session.Id)
                     && (x.Event.LeadOrganiserId == personId
                      || x.Event.CoOrganisers.Any(c => c.PersonId == personId)))
            .Select(x =>
            {
                organiserSessionIds.Add(x.Session.Id);
                return new MyScheduleItemDto(
                    x.Session.Id.Value, null,
                    x.Event.Title ?? "",
                    x.Session.TimeSlot.Start, x.Session.TimeSlot.End,
                    venueMap.GetValueOrDefault(x.Session.VenueId),
                    "Organiser", false);
            })
            .ToList();

        // 3. Bevakade sessioner (hoppa över bokade och arrangörssessioner)
        var watches = await db.SessionWatches
            .Where(w => w.PersonId == personId && w.EditionId == editionId)
            .ToListAsync(ct);

        var watched = watches
            .Where(w => activeSessionDict.ContainsKey(w.SessionId)
                     && !bookedSessionIds.Contains(w.SessionId)
                     && !organiserSessionIds.Contains(w.SessionId))
            .Select(w =>
            {
                var (evt, s) = activeSessionDict[w.SessionId];
                return new MyScheduleItemDto(
                    s.Id.Value, null,
                    evt.Title ?? "",
                    s.TimeSlot.Start, s.TimeSlot.End,
                    venueMap.GetValueOrDefault(s.VenueId),
                    "Watching", false);
            })
            .ToList();

        // 4. Bemanningspass – krosskontext via Edition.Stations-navigering
        var stationIds = await db.Editions
            .Where(e => e.Id == editionId)
            .SelectMany(e => e.Stations)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var shiftItems = new List<MyScheduleItemDto>();
        if (stationIds.Count > 0)
        {
            var shifts = await db.Shifts
                .Include(s => s.Assignments)
                .Where(s => stationIds.Contains(s.StationId)
                         && s.Status != ShiftStatus.Cancelled
                         && s.Assignments.Any(a => a.PersonId == personId
                             && (a.Status == StaffAssignmentStatus.Assigned
                              || a.Status == StaffAssignmentStatus.Confirmed)))
                .ToListAsync(ct);

            var usedStationIds = shifts.Select(s => s.StationId).ToHashSet();
            var stationNameMap = usedStationIds.Count > 0
                ? await db.Set<Station>()
                    .Where(s => usedStationIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
                : new Dictionary<StationId, string>();

            shiftItems = shifts
                .Select(s => new MyScheduleItemDto(
                    null, s.Id.Value,
                    stationNameMap.GetValueOrDefault(s.StationId) ?? "",
                    s.TimeSlot.Start, s.TimeSlot.End,
                    stationNameMap.GetValueOrDefault(s.StationId),
                    "Shift", true))
                .ToList();
        }

        return booked.Concat(organiser).Concat(watched).Concat(shiftItems)
            .OrderBy(i => i.Start)
            .ToList();
    }
}
