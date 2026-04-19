namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface ITenantResolver
{
    Task<ResolvedTenant?> ResolveBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<ResolvedTenant?> ResolveByIdAsync(Guid tenantId, CancellationToken ct = default);
}
