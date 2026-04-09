using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EditionRepository(ConventionDbContext db) : IEditionRepository
{
    public async Task AddAndSaveAsync(Edition edition, CancellationToken ct = default)
    {
        await db.Editions.AddAsync(edition, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<Edition?> GetByIdAsync(EditionId id, CancellationToken ct = default)
        => db.Editions.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
