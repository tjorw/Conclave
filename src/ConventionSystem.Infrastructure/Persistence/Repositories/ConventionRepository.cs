using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class ConventionRepository(ConventionDbContext db) : IConventionRepository
{
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => db.Conventions.AnyAsync(c => c.Slug == slug, ct);

    public async Task CreateWithAdminAsync(
        Domain.Convention.Aggregates.Convention convention,
        Person admin,
        CancellationToken ct = default)
    {
        await db.Conventions.AddAsync(convention, ct);
        await db.Persons.AddAsync(admin, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<Domain.Convention.Aggregates.Convention?> GetByIdAsync(ConventionId id, CancellationToken ct = default)
        => db.Conventions
            .Include(c => c.Administrators)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
