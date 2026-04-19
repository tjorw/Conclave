using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(ConventionDbContext db) : ITenantRepository
{
    public Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct = default)
        => db.Tenants.AnyAsync(t => t.Subdomain == subdomain, ct);

    public Task AddAsync(Tenant tenant, CancellationToken ct = default)
        => db.Tenants.AddAsync(tenant, ct).AsTask();

    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TenantListItemDto>> ListAsync(CancellationToken ct = default)
        => await db.Tenants
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TenantListItemDto(t.Id.Value, t.Subdomain, t.DisplayName, t.Status, t.CreatedAt))
            .ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}