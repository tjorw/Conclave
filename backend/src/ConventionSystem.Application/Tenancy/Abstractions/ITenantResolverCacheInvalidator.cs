namespace ConventionSystem.Application.Tenancy.Abstractions;

public interface ITenantResolverCacheInvalidator
{
    Task InvalidateAsync(Guid tenantId, string? subdomain = null, CancellationToken ct = default);
}