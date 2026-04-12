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
        // EF Core kan inte lösa insert-ordningen när en cirkulär FK-cykel innehåller
        // ett obligatoriskt beroende. Här är cykeln:
        //   EventVersion.event_id (required) → events
        //   Event.draft_version_id (optional) → event_versions
        //
        // Lösning: spara Event med draft_version_id = null i steg 1 (EF Core infogar då
        // Event före EventVersion), sätt sedan draft_version_id i steg 2.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Events.Add(ev);

        var draftId = ev.DraftVersionId;
        db.Entry(ev).Property(e => e.DraftVersionId).CurrentValue = null;

        await db.SaveChangesAsync(ct);

        if (draftId is not null)
        {
            db.Entry(ev).Property(e => e.DraftVersionId).CurrentValue = draftId;
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default)
        => db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithDraftVersionAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Versions)
                .ThenInclude(v => v.SessionRequests)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<EventSummaryDto>> ListByEditionIdAsync(EditionId id, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Versions)
            .Include(e => e.Sessions)
            .Where(e => e.EditionId == id)
            .ToListAsync(ct);

        // Cross-context join: kategorier och arrangörsnamn via delad DB
        var categoryNames = await db.Categories
            .Where(c => EF.Property<EditionId>(c, "EditionId") == id)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var organiserIds = events.Select(e => e.LeadOrganiserId).Distinct().ToHashSet();
        var organiserNames = await db.Persons
            .Where(p => organiserIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return events.Select(e =>
        {
            var displayVersion = e.PublishedVersionId.HasValue
                ? e.Versions.FirstOrDefault(v => v.Id == e.PublishedVersionId.Value)
                : e.DraftVersionId.HasValue
                    ? e.Versions.FirstOrDefault(v => v.Id == e.DraftVersionId.Value)
                    : null;

            return new EventSummaryDto(
                e.Id.Value,
                e.EditionId.Value,
                e.CategoryId.Value,
                categoryNames.GetValueOrDefault(e.CategoryId),
                e.LeadOrganiserId.Value,
                organiserNames.GetValueOrDefault(e.LeadOrganiserId),
                e.Status.ToString(),
                displayVersion?.Title,
                e.Sessions.Count(s => s.Status == Domain.Event.Enums.SessionStatus.Active));
        }).ToList();
    }

    public async Task<EventDto?> GetProjectedByIdAsync(EventId id, CancellationToken ct = default)
    {
        var ev = await db.Events
            .Include(e => e.Versions)
                .ThenInclude(v => v.SessionRequests)
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev is null) return null;

        var organiserName = await db.Persons
            .Where(p => p.Id == ev.LeadOrganiserId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);

        EventVersionDto MapVersion(Domain.Event.Entities.EventVersion v) => new(
            v.Id.Value, v.Title, v.Description,
            v.RegistrationType.ToString(), v.DropInRules, v.Status.ToString(),
            v.CreatedAt,
            v.SessionRequests.Select(r => new SessionRequestDto(
                r.Id.Value, r.Description, r.RequestedDurationMinutes,
                r.RequestedSeats, r.StartType.ToString())).ToList());

        var publishedVersion = ev.PublishedVersionId.HasValue
            ? ev.Versions.FirstOrDefault(v => v.Id == ev.PublishedVersionId.Value)
            : null;

        var draftVersion = ev.DraftVersionId.HasValue
            ? ev.Versions.FirstOrDefault(v => v.Id == ev.DraftVersionId.Value)
            : null;

        return new EventDto(
            ev.Id.Value,
            ev.EditionId.Value,
            ev.CategoryId.Value,
            ev.LeadOrganiserId.Value,
            organiserName,
            ev.Status.ToString(),
            ev.CoOrganisers.Select(c => c.PersonId.Value).ToList(),
            publishedVersion is not null ? MapVersion(publishedVersion) : null,
            draftVersion is not null ? MapVersion(draftVersion) : null,
            ev.Sessions.Select(s => new SessionDto(
                s.Id.Value, s.VenueId.Value,
                s.TimeSlot.Start, s.TimeSlot.End,
                s.MaxSeats, s.StartType.ToString(), s.Status.ToString())).ToList());
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
