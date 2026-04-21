using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
}

public sealed class DefaultTenantContext(
    IHttpContextAccessor httpContextAccessor,
    IAmbientTenantContext ambientTenantContext,
    IOptions<MultitenancyOptions> options) : ITenantContext
{
    private const string SystemPathPrefix = "/system";

    public Guid TenantId
    {
        get
        {
            if (ambientTenantContext.TenantId is Guid ambientTenantId)
                return ambientTenantId;

            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext is null)
                return Guid.Empty;

            if (httpContext.Items.TryGetValue(TenantContextItemKeys.TenantId, out var value)
                && value is Guid tenantId)
                return tenantId;

            if (httpContext.Request.Path.StartsWithSegments(SystemPathPrefix, StringComparison.OrdinalIgnoreCase))
                return Guid.Empty;

            if (!options.Value.Enabled)
                return Guid.Empty;

            throw new InvalidOperationException(
                "Tenant-ID saknas i förfrågan. TenantResolutionMiddleware har inte körts.");
        }
    }
}
