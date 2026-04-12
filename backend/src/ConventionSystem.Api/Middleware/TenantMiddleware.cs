using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private const string ConventionIdHeader = "X-Convention-Id";

    public async Task InvokeAsync(HttpContext context, SystemDbContext systemDb, TenantContext tenantContext)
    {
        Guid? conventionId = null;

        if (context.Request.Headers.TryGetValue(ConventionIdHeader, out var headerValue)
            && Guid.TryParse(headerValue, out var fromHeader))
        {
            conventionId = fromHeader;
        }
        else if (context.GetRouteValue("conventionId") is string routeValue
            && Guid.TryParse(routeValue, out var fromRoute))
        {
            conventionId = fromRoute;
        }

        if (conventionId.HasValue)
        {
            var tenant = await systemDb.Tenants.FindAsync(conventionId.Value);
            if (tenant is not null)
                tenantContext.Resolve(conventionId.Value, tenant.ConnectionString);
        }

        await next(context);
    }
}
