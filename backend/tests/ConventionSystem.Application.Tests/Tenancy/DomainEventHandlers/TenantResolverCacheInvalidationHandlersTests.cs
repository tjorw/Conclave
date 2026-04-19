using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.DomainEventHandlers;
using ConventionSystem.Domain.Tenancy.Events;
using ConventionSystem.Domain.Tenancy.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Tenancy.DomainEventHandlers;

public sealed class TenantResolverCacheInvalidationHandlersTests
{
    private readonly ITenantResolverCacheInvalidator _cacheInvalidator = Substitute.For<ITenantResolverCacheInvalidator>();

    [Fact]
    public async Task TenantCreatedHandler_InvalidatesByIdAndSubdomain()
    {
        var tenantId = TenantId.New();
        var handler = new InvalidateTenantResolverCacheOnTenantCreatedHandler(_cacheInvalidator);
        var domainEvent = new TenantCreated(tenantId, "mycon", "My Convention", DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, default);

        await _cacheInvalidator.Received(1)
            .InvalidateAsync(tenantId.Value, "mycon", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantSuspendedHandler_InvalidatesById()
    {
        var tenantId = TenantId.New();
        var handler = new InvalidateTenantResolverCacheOnTenantSuspendedHandler(_cacheInvalidator);
        var domainEvent = new TenantSuspended(tenantId, DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, default);

        await _cacheInvalidator.Received(1)
            .InvalidateAsync(tenantId.Value, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TenantRestoredHandler_InvalidatesById()
    {
        var tenantId = TenantId.New();
        var handler = new InvalidateTenantResolverCacheOnTenantRestoredHandler(_cacheInvalidator);
        var domainEvent = new TenantRestored(tenantId, DateTimeOffset.UtcNow);

        await handler.Handle(domainEvent, default);

        await _cacheInvalidator.Received(1)
            .InvalidateAsync(tenantId.Value, null, Arg.Any<CancellationToken>());
    }
}