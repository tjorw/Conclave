using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Tenancy.Commands.CreateTenant;
using ConventionSystem.Application.Tenancy.Commands.RestoreTenant;
using ConventionSystem.Application.Tenancy.Commands.SuspendTenant;
using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class SystemTenantEndpoints
{
    public static IEndpointRouteBuilder MapSystemTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/system/tenants")
            .RequireAuthorization(AuthConstants.Policies.IsSystemAdmin);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var tenants = await sender.Send(new ListTenantsQuery(), ct);
            return Results.Ok(tenants);
        });

        group.MapPost("/", async (CreateSystemTenantRequest request, ISender sender, CancellationToken ct) =>
        {
            var tenantId = await sender.Send(new CreateTenantCommand(request.Subdomain, request.DisplayName), ct);
            return Results.Created($"/system/tenants/{tenantId}", new { id = tenantId });
        });

        group.MapPut("/{tenantId:guid}/suspend", async (Guid tenantId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SuspendTenantCommand(tenantId), ct);
            return Results.NoContent();
        });

        group.MapPut("/{tenantId:guid}/restore", async (Guid tenantId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RestoreTenantCommand(tenantId), ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record CreateSystemTenantRequest(string Subdomain, string DisplayName);