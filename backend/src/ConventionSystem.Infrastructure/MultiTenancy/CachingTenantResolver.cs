using ConventionSystem.Domain.Tenancy.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class CachingTenantResolver(
    IDbContextFactory<TenantLookupDbContext> dbContextFactory,
    IMemoryCache cache) : ITenantResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public Task<ResolvedTenant?> ResolveBySubdomainAsync(string subdomain, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(
            $"tenant:subdomain:{subdomain.ToLowerInvariant()}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                await using var db = await dbContextFactory.CreateDbContextAsync(ct);
                return await db.Tenants
                    .Where(t => t.Subdomain == subdomain)
                    .Select(t => new ResolvedTenant(t.Id.Value, t.Status))
                    .FirstOrDefaultAsync(ct);
            })!;

    public Task<ResolvedTenant?> ResolveByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(
            $"tenant:id:{tenantId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                await using var db = await dbContextFactory.CreateDbContextAsync(ct);
                var id = new TenantId(tenantId);
                return await db.Tenants
                    .Where(t => t.Id == id)
                    .Select(t => new ResolvedTenant(t.Id.Value, t.Status))
                    .FirstOrDefaultAsync(ct);
            })!;
}
