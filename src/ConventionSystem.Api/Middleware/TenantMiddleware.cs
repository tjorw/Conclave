using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private const string ConventionIdHeader = "X-Convention-Id";

    public async Task InvokeAsync(HttpContext context, SystemDbContext systemDb, TenantContext tenantContext)
    {
        if (context.Request.Headers.TryGetValue(ConventionIdHeader, out var value)
            && Guid.TryParse(value, out var conventionId))
        {
            var tenant = await systemDb.Tenants.FindAsync(conventionId);
            if (tenant is not null)
                tenantContext.Resolve(conventionId, tenant.ConnectionString);
        }

        await next(context);
    }
}
