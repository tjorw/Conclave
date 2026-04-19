using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class TenantSeedInterceptor(
    ITenantContext tenantContext,
    IOptions<MultitenancyOptions> options) : SaveChangesInterceptor
{
    private const string TenantIdPropertyName = "TenantId";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var tenantId = tenantContext.TenantId;
        if (!options.Value.Enabled && tenantId == Guid.Empty)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
                continue;

            var tenantProperty = entry.Metadata.FindProperty(TenantIdPropertyName);
            if (tenantProperty is null)
                continue;

            entry.Property(TenantIdPropertyName).CurrentValue = tenantId;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}