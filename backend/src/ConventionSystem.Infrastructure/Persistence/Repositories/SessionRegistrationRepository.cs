using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class SessionRegistrationRepository(ConventionDbContext db) : ISessionRegistrationRepository
{
    public Task<SessionRegistration?> GetByIdAsync(SessionRegistrationId id, CancellationToken ct = default)
        => db.SessionRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedBySessionIdAsync(SessionId sessionId, CancellationToken ct = default)
        => await db.SessionRegistrations
            .Where(r => r.SessionId == sessionId
                     && r.Status == Domain.Registration.Enums.SessionRegistrationStatus.Confirmed)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<SessionId, int>> CountConfirmedBySessionIdsAsync(
        IReadOnlyCollection<SessionId> sessionIds,
        CancellationToken ct = default)
    {
        if (sessionIds.Count == 0)
        {
            return new Dictionary<SessionId, int>();
        }

        var counts = await db.SessionRegistrations
            .Where(r => sessionIds.Contains(r.SessionId)
                     && r.Status == SessionRegistrationStatus.Confirmed)
            .GroupBy(r => r.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.SessionId, x => x.Count);
    }

    public Task<bool> HasRegistrationAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default)
        => db.SessionRegistrations.AnyAsync(
            r => r.PersonId == personId && r.SessionId == sessionId
              && r.Status != Domain.Registration.Enums.SessionRegistrationStatus.Cancelled, ct);

    public async Task<IReadOnlyList<MySessionRegistrationSummaryDto>> ListByPersonAndEditionAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var registrations = await db.SessionRegistrations
            .Where(r => r.PersonId == personId && r.Status != SessionRegistrationStatus.Cancelled)
            .ToListAsync(ct);

        if (registrations.Count == 0) return [];

        var sessionIds = registrations.Select(r => r.SessionId).ToHashSet();

        var events = await db.Events
            .Include(e => e.Sessions)
            .Where(e => e.EditionId == editionId)
            .ToListAsync(ct);

        var sessionLookup = events
            .SelectMany(e => e.Sessions.Select(s => new { EventTitle = e.Title, Session = s }))
            .Where(x => sessionIds.Contains(x.Session.Id))
            .ToDictionary(x => x.Session.Id);

        if (sessionLookup.Count == 0) return [];

        var venueIds = sessionLookup.Values.Select(x => x.Session.VenueId).ToHashSet();
        var venueMap = await db.Set<Venue>()
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, ct);

        return registrations
            .Where(r => sessionLookup.ContainsKey(r.SessionId))
            .OrderBy(r => sessionLookup[r.SessionId].Session.TimeSlot.Start)
            .Select(r =>
            {
                var x = sessionLookup[r.SessionId];
                return new MySessionRegistrationSummaryDto(
                    r.Id.Value,
                    r.SessionId.Value,
                    x.EventTitle ?? "",
                    x.Session.TimeSlot.Start,
                    x.Session.TimeSlot.End,
                    venueMap.GetValueOrDefault(x.Session.VenueId) ?? "",
                    r.Status.ToString());
            }).ToList();
    }

    public async Task AddAndSaveAsync(SessionRegistration registration, CancellationToken ct = default)
    {
        db.SessionRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
