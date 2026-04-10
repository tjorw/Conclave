using ConventionSystem.Application.Event.Abstractions;
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

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
