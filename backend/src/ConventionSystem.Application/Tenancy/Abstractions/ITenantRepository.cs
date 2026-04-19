using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;

namespace ConventionSystem.Application.Tenancy.Abstractions;

public interface ITenantRepository
{
    Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default);
    Task<IReadOnlyList<TenantListItemDto>> ListAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}