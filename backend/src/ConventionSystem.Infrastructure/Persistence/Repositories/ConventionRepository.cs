using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
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

    public Task<Domain.Convention.Aggregates.Convention?> GetSingleAsync(CancellationToken ct = default)
        => db.Conventions
            .Include(c => c.Administrators)
            .OrderBy(c => EF.Property<Guid>(c, "TenantId"))
            .FirstOrDefaultAsync(ct);

    public Task<ConventionDto?> GetProjectedByIdAsync(ConventionId id, CancellationToken ct = default)
        => db.Conventions
            .Where(c => c.Id == id)
            .Select(c => new ConventionDto(
                c.Id.Value,
                c.Name,
                c.Slug,
                c.ActiveEditionId.HasValue ? c.ActiveEditionId.Value.Value : null))
            .FirstOrDefaultAsync(ct);

    public Task<ConventionDto?> GetProjectedAsync(CancellationToken ct = default)
        => db.Conventions
            .Select(c => new ConventionDto(
                c.Id.Value,
                c.Name,
                c.Slug,
                c.ActiveEditionId.HasValue ? c.ActiveEditionId.Value.Value : null))
            .FirstOrDefaultAsync(ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task<EditionId?> GetActiveEditionIdAsync(CancellationToken ct = default)
    {
        var convention = await db.Conventions.FirstOrDefaultAsync(ct);
        return convention?.ActiveEditionId;
    }
}
