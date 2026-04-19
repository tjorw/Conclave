using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using ConventionSystem.Infrastructure.Persistence;

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

        var entriesToSeed = eventData.Context.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added &&
                            entry.Metadata.FindProperty(TenantIdPropertyName) is not null)
            .ToList();

        if (entriesToSeed.Count == 0)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var requiresResolvedTenant = entriesToSeed
            .Any(entry => entry.Entity is not DomainEventLogEntry);

        if (!requiresResolvedTenant)
        {
            foreach (var entry in entriesToSeed)
                entry.Property(TenantIdPropertyName).CurrentValue = Guid.Empty;

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var tenantId = tenantContext.TenantId;
        if (!options.Value.Enabled && tenantId == Guid.Empty)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in entriesToSeed)
        {
            entry.Property(TenantIdPropertyName).CurrentValue = tenantId;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}