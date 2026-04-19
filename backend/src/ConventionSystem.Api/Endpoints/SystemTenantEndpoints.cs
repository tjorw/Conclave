using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.CreateTenant;
using ConventionSystem.Application.Tenancy.Commands.RestoreTenant;
using ConventionSystem.Application.Tenancy.Commands.SuspendTenant;
using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using MediatR;
using Microsoft.AspNetCore.Identity;

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

        group.MapPost("/{tenantId:guid}/provision", async (
            Guid tenantId,
            ProvisionTenantConventionRequest request,
            HttpContext httpContext,
            ISender sender,
            ITenantRepository tenantRepository,
            IPersonRepository personRepository,
            TenantAwareUserService tenantAwareUserService,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var tenant = await tenantRepository.GetByIdAsync(new TenantId(tenantId), ct);
            if (tenant is null)
                return Results.NotFound();

            var existingUser = await tenantAwareUserService.FindTenantUserAsync(request.AdminEmail, tenantId, ct);
            if (existingUser is not null)
            {
                return Results.Problem(
                    title: "E-postadressen används redan.",
                    statusCode: 422,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = "email_already_exists"
                    });
            }

            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var conventionId = await sender.Send(
                new CreateConventionCommand(
                    request.ConventionName,
                    request.ConventionSlug,
                    request.AdminName,
                    request.AdminEmail),
                ct);

            var person = await personRepository.FindByEmailInConventionAsync(
                new ConventionId(conventionId),
                request.AdminEmail,
                ct);

            if (person is null)
                return Results.Problem("Kunde inte skapa admin-person för konventet.", statusCode: 422);

            var user = new ApplicationUser
            {
                UserName = $"{tenantId:N}_{request.AdminEmail}",
                Email = request.AdminEmail,
                UserType = UserType.TenantUser,
                TenantId = tenantId,
                PersonId = person.Id.Value,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, request.AdminPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                {
                    return Results.Problem(
                        title: "E-postadressen används redan.",
                        statusCode: 422,
                        extensions: new Dictionary<string, object?>
                        {
                            ["errorCode"] = "email_already_exists"
                        });
                }

                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return Results.Problem(errors, statusCode: 400);
            }

            return Results.Created(
                $"/system/tenants/{tenantId}/provision/{conventionId}",
                new
                {
                    conventionId,
                    adminUserId = user.Id
                });
        });

        return app;
    }
}

public record CreateSystemTenantRequest(string Subdomain, string DisplayName);
public record ProvisionTenantConventionRequest(
    string ConventionName,
    string ConventionSlug,
    string AdminName,
    string AdminEmail,
    string AdminPassword);