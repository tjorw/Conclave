using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Events;
using ConventionSystem.Domain.Tenancy.Exceptions;
using ConventionSystem.Domain.Tenancy.Ids;

namespace ConventionSystem.Domain.Tests.Tenancy;

public class TenantTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesActiveTenantAndRaisesEvent()
    {
        var tenantId = TenantId.New();

        var tenant = new Tenant(tenantId, " gamma-con ", " Gamma Con ");

        Assert.Equal(tenantId, tenant.Id);
        Assert.Equal("gamma-con", tenant.Subdomain);
        Assert.Equal("Gamma Con", tenant.DisplayName);
        Assert.Equal(TenantStatus.Active, tenant.Status);

        var domainEvent = Assert.Single(tenant.DomainEvents.OfType<TenantCreated>());
        Assert.Equal(tenant.Id, domainEvent.TenantId);
        Assert.Equal("gamma-con", domainEvent.Subdomain);
        Assert.Equal("Gamma Con", domainEvent.DisplayName);
    }

    [Fact]
    public void Constructor_InvalidSubdomain_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Tenant(TenantId.New(), "Invalid_Subdomain", "Gamma Con"));
    }

    [Fact]
    public void Suspend_ActiveTenant_SetsSuspendedAndRaisesEvent()
    {
        var tenant = new Tenant(TenantId.New(), "gammacon", "Gamma Con");
        tenant.ClearDomainEvents();

        tenant.Suspend();

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        var domainEvent = Assert.Single(tenant.DomainEvents.OfType<TenantSuspended>());
        Assert.Equal(tenant.Id, domainEvent.TenantId);
    }

    [Fact]
    public void Suspend_AlreadySuspended_Throws()
    {
        var tenant = new Tenant(TenantId.New(), "gammacon", "Gamma Con");
        tenant.Suspend();

        Assert.Throws<TenantAlreadySuspendedException>(() => tenant.Suspend());
    }

    [Fact]
    public void Restore_SuspendedTenant_SetsActiveAndRaisesEvent()
    {
        var tenant = new Tenant(TenantId.New(), "gammacon", "Gamma Con");
        tenant.Suspend();
        tenant.ClearDomainEvents();

        tenant.Restore();

        Assert.Equal(TenantStatus.Active, tenant.Status);
        var domainEvent = Assert.Single(tenant.DomainEvents.OfType<TenantRestored>());
        Assert.Equal(tenant.Id, domainEvent.TenantId);
    }

    [Fact]
    public void Restore_AlreadyActive_Throws()
    {
        var tenant = new Tenant(TenantId.New(), "gammacon", "Gamma Con");

        Assert.Throws<TenantAlreadyActiveException>(() => tenant.Restore());
    }
}