using System.Security.Claims;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class DefaultTenantContextTests
{
    [Fact]
    public void TenantId_WhenMultitenancyIsDisabled_UsesTenantClaim()
    {
        var tenantId = Guid.CreateVersion7();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tenantId.ToString())],
                authenticationType: "Test"))
        };

        var tenantContext = new DefaultTenantContext(
            new HttpContextAccessor { HttpContext = httpContext },
            new AmbientTenantContext(),
            Options.Create(new MultitenancyOptions { Enabled = false }));

        Assert.Equal(tenantId, tenantContext.TenantId);
    }

    [Fact]
    public void TenantId_WhenMultitenancyIsEnabled_DoesNotTrustTenantClaimWithoutResolvedTenant()
    {
        var tenantId = Guid.CreateVersion7();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tenantId.ToString())],
                authenticationType: "Test"))
        };

        var tenantContext = new DefaultTenantContext(
            new HttpContextAccessor { HttpContext = httpContext },
            new AmbientTenantContext(),
            Options.Create(new MultitenancyOptions { Enabled = true }));

        Assert.Throws<InvalidOperationException>(() => tenantContext.TenantId);
    }
}
