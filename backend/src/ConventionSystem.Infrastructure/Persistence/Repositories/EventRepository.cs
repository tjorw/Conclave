using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(ConventionDbContext db) : IEventRepository
{
    public async Task AddAndSaveAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
    }

    public Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default)
        => db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionRequestsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.SessionRequests)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public void MarkAsAdded<T>(T entity) where T : class
        => db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    public async Task<IReadOnlyList<EventSummaryDto>> ListByEditionIdAsync(EditionId id, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Where(e => e.EditionId == id)
            .ToListAsync(ct);

        var categoryNames = await db.Categories
            .Where(c => EF.Property<EditionId>(c, "EditionId") == id)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var organiserIds = events.Select(e => e.LeadOrganiserId).Distinct().ToHashSet();
        var organiserNames = await db.Persons
            .Where(p => organiserIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return events.Select(e => new EventSummaryDto(
            e.Id.Value,
            e.EditionId.Value,
            e.CategoryId.Value,
            categoryNames.GetValueOrDefault(e.CategoryId),
            e.LeadOrganiserId.Value,
            organiserNames.GetValueOrDefault(e.LeadOrganiserId),
            e.Status.ToString(),
            string.IsNullOrEmpty(e.Title) ? null : e.Title,
            e.Sessions.Count(s => s.Status == Domain.Event.Enums.SessionStatus.Active)
        )).ToList();
    }

    public async Task<EventDto?> GetProjectedByIdAsync(EventId id, CancellationToken ct = default)
    {
        var ev = await db.Events
            .Include(e => e.SessionRequests)
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Include(e => e.Comments)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev is null) return null;

        var organiserName = await db.Persons
            .Where(p => p.Id == ev.LeadOrganiserId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);

        return new EventDto(
            ev.Id.Value,
            ev.EditionId.Value,
            ev.CategoryId.Value,
            ev.LeadOrganiserId.Value,
            organiserName,
            ev.Status.ToString(),
            ev.Title,
            ev.Description,
            ev.RegistrationType.ToString(),
            ev.DropInRules,
            ev.CoOrganisers.Select(c => c.PersonId.Value).ToList(),
            ev.SessionRequests.Select(r => new SessionRequestDto(
                r.Id.Value, r.Description, r.RequestedDurationMinutes,
                r.RequestedSeats, r.StartType.ToString())).ToList(),
            ev.Sessions.Select(s => new SessionDto(
                s.Id.Value, s.VenueId.Value,
                s.TimeSlot.Start, s.TimeSlot.End,
                s.MaxSeats, s.StartType.ToString(), s.Status.ToString())).ToList(),
            ev.Comments.Select(c => new EventCommentDto(
                c.Id.Value, c.AuthorId.Value, c.Text, c.CreatedAt)).ToList());
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
