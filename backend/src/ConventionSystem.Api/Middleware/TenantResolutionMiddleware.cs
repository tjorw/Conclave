using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Api.Middleware;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IOptions<MultitenancyOptions> multitenancyOptions)
{
    private const string TenantIdHeader = "X-Tenant-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!multitenancyOptions.Value.Enabled)
        {
            await next(context);
            return;
        }

        var tenant = await ResolveTenantAsync(context);
        if (tenant is null)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                "Tenanten kunde inte hittas.",
                "tenant_not_found");
            return;
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Tenanten är suspenderad.",
                "tenant_suspended");
            return;
        }

        context.Items[TenantContextItemKeys.TenantId] = tenant.Id;
        await next(context);
    }

    private async Task<ResolvedTenant?> ResolveTenantAsync(HttpContext context)
    {
        var subdomain = TryExtractSubdomain(context.Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(subdomain))
            return await GetTenantBySubdomainAsync(subdomain);

        if (!hostEnvironment.IsDevelopment())
            return null;

        var tenantIdHeaderValue = context.Request.Headers[TenantIdHeader].ToString();
        if (!Guid.TryParse(tenantIdHeaderValue, out var tenantId))
            return null;

        return await GetTenantByIdAsync(tenantId);
    }

    private async Task<ResolvedTenant?> GetTenantBySubdomainAsync(string subdomain)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Anslutningsstrang saknas for DefaultConnection.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP (1) [Id], [Status] FROM [tenants] WHERE [Subdomain] = @subdomain";
        command.Parameters.AddWithValue("@subdomain", subdomain);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new ResolvedTenant(
            reader.GetGuid(0),
            Enum.Parse<TenantStatus>(reader.GetString(1), ignoreCase: true));
    }

    private async Task<ResolvedTenant?> GetTenantByIdAsync(Guid tenantId)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Anslutningsstrang saknas for DefaultConnection.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP (1) [Id], [Status] FROM [tenants] WHERE [Id] = @tenantId";
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new ResolvedTenant(
            reader.GetGuid(0),
            Enum.Parse<TenantStatus>(reader.GetString(1), ignoreCase: true));
    }

    private static string? TryExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return null;

        if (Uri.CheckHostName(host) == UriHostNameType.IPv4 || Uri.CheckHostName(host) == UriHostNameType.IPv6)
            return null;

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
            return null;

        return segments[0].ToLowerInvariant();
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

    private sealed record ResolvedTenant(Guid Id, TenantStatus Status);
}