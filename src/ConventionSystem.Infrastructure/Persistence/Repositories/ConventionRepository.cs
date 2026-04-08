using ConventionSystem.Application.Convention.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class ConventionRepository(ConventionDbContext db) : IConventionRepository
{
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => db.Conventions.AnyAsync(c => c.Slug == slug, ct);

    public async Task AddAsync(Domain.Convention.Aggregates.Convention convention, CancellationToken ct = default)
    {
        await db.Conventions.AddAsync(convention, ct);
        await db.SaveChangesAsync(ct);
    }
}
