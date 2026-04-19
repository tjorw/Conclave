using ConventionSystem.Application.Common;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Tenancy.Events;

namespace ConventionSystem.Application.Tenancy.DomainEventHandlers;

public sealed class InvalidateTenantResolverCacheOnTenantCreatedHandler(
    ITenantResolverCacheInvalidator cacheInvalidator) : IDomainEventHandler<TenantCreated>
{
    public async Task Handle(TenantCreated notification, CancellationToken ct)
    {
        await cacheInvalidator.InvalidateAsync(notification.TenantId.Value, notification.Subdomain, ct);
    }
}

public sealed class InvalidateTenantResolverCacheOnTenantSuspendedHandler(
    ITenantResolverCacheInvalidator cacheInvalidator) : IDomainEventHandler<TenantSuspended>
{
    public async Task Handle(TenantSuspended notification, CancellationToken ct)
    {
        await cacheInvalidator.InvalidateAsync(notification.TenantId.Value, ct: ct);
    }
}

public sealed class InvalidateTenantResolverCacheOnTenantRestoredHandler(
    ITenantResolverCacheInvalidator cacheInvalidator) : IDomainEventHandler<TenantRestored>
{
    public async Task Handle(TenantRestored notification, CancellationToken ct)
    {
        await cacheInvalidator.InvalidateAsync(notification.TenantId.Value, ct: ct);
    }
}