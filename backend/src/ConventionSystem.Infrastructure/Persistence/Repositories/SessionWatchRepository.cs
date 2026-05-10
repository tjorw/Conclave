using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class SessionWatchRepository(ConventionDbContext db) : ISessionWatchRepository
{
    public async Task<EditionId?> FindEditionIdBySessionIdAsync(SessionId sessionId, CancellationToken ct = default)
    {
        var editionId = await db.Events
            .Where(e => e.Sessions.Any(s => s.Id == sessionId && s.Status == SessionStatus.Active))
            .Select(e => e.EditionId)
            .FirstOrDefaultAsync(ct);

        return editionId == default ? null : editionId;
    }

    public Task<bool> ExistsAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default)
        => db.SessionWatches.AnyAsync(w => w.PersonId == personId && w.SessionId == sessionId, ct);

    public async Task<IReadOnlyList<MyWatchedSessionSummaryDto>> ListByPersonAndEditionAsync(
        PersonId personId,
        EditionId editionId,
        CancellationToken ct = default)
    {
        var watches = await db.SessionWatches
            .Where(w => w.PersonId == personId && w.EditionId == editionId)
            .ToListAsync(ct);

        if (watches.Count == 0) return [];

        var watchedSessionIds = watches.Select(w => w.SessionId).ToHashSet();

        var events = await db.Events
            .Include(e => e.Sessions)
            .Where(e => e.EditionId == editionId)
            .ToListAsync(ct);

        var sessionLookup = events
            .SelectMany(e => e.Sessions.Select(s => new { EventTitle = e.Title, Session = s }))
            .Where(x => watchedSessionIds.Contains(x.Session.Id) && x.Session.Status == SessionStatus.Active)
            .ToDictionary(x => x.Session.Id);

        if (sessionLookup.Count == 0) return [];

        var venueIds = sessionLookup.Values.Select(x => x.Session.VenueId).ToHashSet();
        var venueMap = await db.Venues
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, ct);

        return watches
            .Where(w => sessionLookup.ContainsKey(w.SessionId))
            .Select(w =>
            {
                var x = sessionLookup[w.SessionId];
                return new MyWatchedSessionSummaryDto(
                    w.SessionId.Value,
                    x.EventTitle ?? string.Empty,
                    x.Session.TimeSlot.Start,
                    x.Session.TimeSlot.End,
                    venueMap.GetValueOrDefault(x.Session.VenueId) ?? string.Empty,
                    w.CreatedAt);
            })
            .OrderBy(s => s.Start)
            .ToList();
    }

    public async Task AddAndSaveAsync(SessionWatch watch, CancellationToken ct = default)
    {
        db.SessionWatches.Add(watch);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveByPersonAndSessionAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default)
    {
        var watch = await db.SessionWatches
            .FirstOrDefaultAsync(w => w.PersonId == personId && w.SessionId == sessionId, ct);

        if (watch is null)
        {
            return;
        }

        db.SessionWatches.Remove(watch);
        await db.SaveChangesAsync(ct);
    }
}
