using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Tenancy.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class CachingTenantResolver(
    IDbContextFactory<TenantLookupDbContext> dbContextFactory,
    IMemoryCache cache) : ITenantResolver, ITenantResolverCacheInvalidator
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ResolvedTenant?> ResolveBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var subdomainCacheKey = BuildSubdomainCacheKey(normalizedSubdomain);

        if (cache.TryGetValue(subdomainCacheKey, out ResolvedTenant? cachedTenant))
            return cachedTenant;

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var tenantData = await db.Tenants
            .Where(t => t.Subdomain == normalizedSubdomain)
            .Select(t => new
            {
                TenantId = t.Id.Value,
                t.Subdomain,
                t.Status
            })
            .FirstOrDefaultAsync(ct);

        if (tenantData is null)
            return null;

        var resolvedTenant = new ResolvedTenant(tenantData.TenantId, tenantData.Status);
        CacheResolvedTenant(resolvedTenant, tenantData.Subdomain);
        return resolvedTenant;
    }

    public async Task<ResolvedTenant?> ResolveByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var idCacheKey = BuildIdCacheKey(tenantId);
        if (cache.TryGetValue(idCacheKey, out ResolvedTenant? cachedTenant))
            return cachedTenant;

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var id = new TenantId(tenantId);
        var tenantData = await db.Tenants
            .Where(t => t.Id == id)
            .Select(t => new
            {
                TenantId = t.Id.Value,
                t.Subdomain,
                t.Status
            })
            .FirstOrDefaultAsync(ct);

        if (tenantData is null)
            return null;

        var resolvedTenant = new ResolvedTenant(tenantData.TenantId, tenantData.Status);
        CacheResolvedTenant(resolvedTenant, tenantData.Subdomain);
        return resolvedTenant;
    }

    public async Task InvalidateAsync(Guid tenantId, string? subdomain = null, CancellationToken ct = default)
    {
        var normalizedSubdomain = NormalizeSubdomain(subdomain)
            ?? cache.Get<string>(BuildTenantSubdomainCacheKey(tenantId));

        if (normalizedSubdomain is null)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var id = new TenantId(tenantId);
            normalizedSubdomain = await db.Tenants
                .Where(t => t.Id == id)
                .Select(t => t.Subdomain)
                .FirstOrDefaultAsync(ct);
            normalizedSubdomain = NormalizeSubdomain(normalizedSubdomain);
        }

        cache.Remove(BuildIdCacheKey(tenantId));
        cache.Remove(BuildTenantSubdomainCacheKey(tenantId));

        if (normalizedSubdomain is null)
            return;

        cache.Remove(BuildSubdomainCacheKey(normalizedSubdomain));
    }

    private void CacheResolvedTenant(ResolvedTenant tenant, string subdomain)
    {
        cache.Set(BuildIdCacheKey(tenant.Id), tenant, CacheTtl);
        cache.Set(BuildSubdomainCacheKey(subdomain), tenant, CacheTtl);
        cache.Set(BuildTenantSubdomainCacheKey(tenant.Id), NormalizeSubdomain(subdomain), CacheTtl);
    }

    private static string BuildSubdomainCacheKey(string subdomain) =>
        $"tenant:subdomain:{NormalizeSubdomain(subdomain)}";

    private static string BuildIdCacheKey(Guid tenantId) => $"tenant:id:{tenantId}";

    private static string BuildTenantSubdomainCacheKey(Guid tenantId) =>
        $"tenant:subdomain-by-id:{tenantId}";

    private static string? NormalizeSubdomain(string? subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return null;

        return subdomain.Trim().ToLowerInvariant();
    }
}
