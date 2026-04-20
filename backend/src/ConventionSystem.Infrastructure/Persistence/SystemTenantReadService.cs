using ConventionSystem.Application.Tenancy.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class SystemTenantReadService(ConventionDbContext db) : ISystemTenantReadService
{
    public async Task<IReadOnlyList<SystemTenantConventionDto>> ListConventionsAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await db.Conventions
            .Where(c => EF.Property<Guid>(c, "TenantId") == tenantId)
            .OrderBy(c => c.Name)
            .Select(c => new SystemTenantConventionDto(c.Id.Value, c.Name, c.Slug))
            .ToListAsync(ct);
    }
}
