using System.Security.Claims;
using ConventionSystem.Api.Auth;
using ConventionSystem.Api.Helpers;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Api.Middleware;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IHostEnvironment hostEnvironment,
    IOptions<MultitenancyOptions> multitenancyOptions,
    ITenantResolver tenantResolver,
    ILogger<TenantResolutionMiddleware> logger)
{
    private const string TenantIdHeader = "X-Tenant-ID";
    private const string SystemPathPrefix = "/system";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/auth/confirm-email", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // System endpoints are intentionally host-level and must not require tenant context.
        if (context.Request.Path.StartsWithSegments(SystemPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!multitenancyOptions.Value.Enabled)
        {
            await next(context);
            return;
        }

        var tenant = await ResolveTenantAsync(context);
        if (tenant is null)
        {
            logger.LogWarning(
                "Tenant resolution failed: tenant_not_found for host {Host} and path {Path}.",
                context.Request.Host.Value,
                context.Request.Path.Value);

            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                "Tenanten kunde inte hittas.",
                "tenant_not_found");
            return;
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            logger.LogWarning(
                "Tenant resolution failed: tenant_suspended for tenant {TenantId}, host {Host} and path {Path}.",
                tenant.Id,
                context.Request.Host.Value,
                context.Request.Path.Value);

            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Tenanten är suspenderad.",
                "tenant_suspended");
            return;
        }

        var jwtTenantId = context.User.FindFirstValue(AuthConstants.Claims.TenantId);
        if (jwtTenantId is not null && Guid.TryParse(jwtTenantId, out var jwtTenantGuid) && jwtTenantGuid != tenant.Id)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Token tillhör en annan tenant.",
                "tenant_mismatch");
            return;
        }

        context.Items[TenantContextItemKeys.TenantId] = tenant.Id;
        await next(context);
    }

    private async Task<ResolvedTenant?> ResolveTenantAsync(HttpContext context)
    {
        var subdomain = HostNameHelpers.TryExtractSubdomain(context.Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(subdomain))
            return await tenantResolver.ResolveBySubdomainAsync(subdomain, context.RequestAborted);

        if (!hostEnvironment.IsDevelopment())
            return null;

        var tenantIdHeaderValue = context.Request.Headers[TenantIdHeader].ToString();
        if (!Guid.TryParse(tenantIdHeaderValue, out var tenantId))
            return null;

        return await tenantResolver.ResolveByIdAsync(tenantId, context.RequestAborted);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string errorCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };
        problem.Extensions["errorCode"] = errorCode;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
