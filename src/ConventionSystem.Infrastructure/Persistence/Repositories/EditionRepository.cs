using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Aggregates;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EditionRepository(ConventionDbContext db) : IEditionRepository
{
    public async Task AddAndSaveAsync(Edition edition, CancellationToken ct = default)
    {
        await db.Editions.AddAsync(edition, ct);
        await db.SaveChangesAsync(ct);
    }
}
