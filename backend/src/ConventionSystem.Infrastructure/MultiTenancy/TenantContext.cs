using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
}

public sealed class DefaultTenantContext(
    IHttpContextAccessor httpContextAccessor,
    IOptions<MultitenancyOptions> options) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            if (httpContextAccessor.HttpContext?.Items.TryGetValue(TenantContextItemKeys.TenantId, out var value) == true
                && value is Guid tenantId)
                return tenantId;

            if (!options.Value.Enabled)
                return Guid.Empty;

            throw new InvalidOperationException(
                "Tenant-ID saknas i förfrågan. TenantResolutionMiddleware har inte körts.");
        }
    }
}
