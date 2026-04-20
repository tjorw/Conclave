using ConventionSystem.Api.Services;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Convention.Queries.ListPersons;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.CreateTenant;
using ConventionSystem.Application.Tenancy.Commands.RestoreTenant;
using ConventionSystem.Application.Tenancy.Commands.SuspendTenant;
using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ConventionSystem.Api.Endpoints;

public static class SystemTenantEndpoints
{
    public static void MapSystemTenantEndpoints(this RouteGroups groups)
    {
        groups.Anonymous.MapPost("/system/signup", async (
            TenantSignupRequest request,
            HttpContext httpContext,
            ISender sender,
            IPersonRepository personRepository,
            TenantAwareUserService tenantAwareUserService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuthLinkBuilder authLinkBuilder,
            ITemporaryPasswordGenerator temporaryPasswordGenerator,
            CancellationToken ct) =>
        {
            var tenantId = await sender.Send(new CreateTenantCommand(request.Subdomain, request.OrganizationName), ct);

            var existingUser = await tenantAwareUserService.FindTenantUserAsync(request.ContactEmail, tenantId, ct);
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
                    request.OrganizationName,
                    request.Subdomain,
                    request.ContactName,
                    request.ContactEmail),
                ct);

            var person = await personRepository.FindByEmailInConventionAsync(
                new ConventionId(conventionId),
                request.ContactEmail,
                ct);

            if (person is null)
                return Results.Problem("Kunde inte skapa kontaktperson för tenanten.", statusCode: 422);

            var temporaryPassword = temporaryPasswordGenerator.Generate();
            var user = new ApplicationUser
            {
                UserName = $"{tenantId:N}_{request.ContactEmail}",
                Email = request.ContactEmail,
                UserType = UserType.TenantUser,
                TenantId = tenantId,
                PersonId = person.Id.Value,
                EmailConfirmed = false
            };

            var createUserResult = await userManager.CreateAsync(user, temporaryPassword);
            if (!createUserResult.Succeeded)
            {
                if (createUserResult.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                {
                    return Results.Problem(
                        title: "E-postadressen används redan.",
                        statusCode: 422,
                        extensions: new Dictionary<string, object?>
                        {
                            ["errorCode"] = "email_already_exists"
                        });
                }

                var errors = string.Join(" ", createUserResult.Errors.Select(e => e.Description));
                return Results.Problem(errors, statusCode: 400);
            }

            await userManager.AddClaimAsync(user, new Claim("activates_tenant", "true"));

            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = authLinkBuilder.BuildSignupConfirmationLink(
                request.ContactEmail,
                tenantId,
                request.Subdomain,
                emailToken);

            await emailService.SendTenantSignupWelcomeAsync(
                request.ContactEmail,
                request.ContactName,
                request.OrganizationName,
                request.Subdomain,
                temporaryPassword,
                confirmLink,
                ct);

            return Results.Created(
                $"/system/tenants/{tenantId}",
                new TenantSignupResponse(tenantId, conventionId, request.ContactEmail, request.Subdomain));
        });

        var group = groups.SystemAdmin.MapGroup("/system/tenants");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var tenants = await sender.Send(new ListTenantsQuery(), ct);
            return Results.Ok(tenants);
        });

        group.MapPost("/", async (
            CreateSystemTenantRequest request,
            ISender sender,
            CancellationToken ct) =>
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

        group.MapGet("/{tenantId:guid}/conventions", async (
            Guid tenantId,
            ISystemTenantReadService readService,
            CancellationToken ct) =>
        {
            var conventions = await readService.ListConventionsAsync(tenantId, ct);
            return Results.Ok(conventions);
        });

        group.MapGet("/{tenantId:guid}/conventions/{conventionId:guid}/persons", async (
            Guid tenantId,
            Guid conventionId,
            HttpContext httpContext,
            ISender sender,
            IConventionRepository conventionRepository,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId), ct);
            if (convention is null)
                return Results.NotFound();

            var persons = await sender.Send(new ListPersonsQuery(conventionId), ct);
            return Results.Ok(persons);
        });

        group.MapPost("/{tenantId:guid}/conventions/{conventionId:guid}/administrators", async (
            Guid tenantId,
            Guid conventionId,
            AddSystemAdministratorRequest request,
            HttpContext httpContext,
            IConventionRepository conventionRepository,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId), ct);
            if (convention is null)
                return Results.NotFound();

            convention.AddAdministrator(new PersonId(request.PersonId), PersonId.New());
            await conventionRepository.SaveAsync(ct);
            return Results.NoContent();
        });

        group.MapDelete("/{tenantId:guid}/conventions/{conventionId:guid}/administrators/{personId:guid}", async (
            Guid tenantId,
            Guid conventionId,
            Guid personId,
            HttpContext httpContext,
            IConventionRepository conventionRepository,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId), ct);
            if (convention is null)
                return Results.NotFound();

            convention.RemoveAdministrator(new PersonId(personId), PersonId.New());
            await conventionRepository.SaveAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/{tenantId:guid}/provision", async (
            Guid tenantId,
            ProvisionTenantConventionRequest request,
            HttpContext httpContext,
            ISender sender,
            ITenantRepository tenantRepository,
            IConventionRepository conventionRepository,
            ISystemTenantReadService systemTenantReadService,
            IPersonRepository personRepository,
            TenantAwareUserService tenantAwareUserService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuthLinkBuilder authLinkBuilder,
            CancellationToken ct) =>
        {
            var tenant = await tenantRepository.GetByIdAsync(new TenantId(tenantId), ct);
            if (tenant is null)
                return Results.NotFound();

            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var existingProvisioning = await FindExistingProvisioningAsync(
                tenantId,
                request.AdminEmail,
                systemTenantReadService,
                conventionRepository,
                personRepository,
                tenantAwareUserService,
                ct);

            if (existingProvisioning is not null)
            {
                return Results.Ok(new ProvisionTenantConventionResponse(
                    existingProvisioning.ConventionId,
                    existingProvisioning.AdminUserId,
                    true));
            }
            var conventionId = await sender.Send(
                new CreateConventionCommand(
                    tenant.DisplayName,
                    tenant.Subdomain,
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

            var loginLink = authLinkBuilder.BuildTenantAdminLoginLink(tenant.Subdomain);
            await emailService.SendTenantProvisionedWelcomeAsync(
                request.AdminEmail,
                request.AdminName,
                tenant.DisplayName,
                tenant.Subdomain,
                request.AdminPassword,
                loginLink,
                ct);

            return Results.Created(
                $"/system/tenants/{tenantId}/provision/{conventionId}",
                new ProvisionTenantConventionResponse(conventionId, user.Id, false));
        });

    }
    private static async Task<ExistingProvisioning?> FindExistingProvisioningAsync(
        Guid tenantId,
        string requestedAdminEmail,
        ISystemTenantReadService systemTenantReadService,
        IConventionRepository conventionRepository,
        IPersonRepository personRepository,
        TenantAwareUserService tenantAwareUserService,
        CancellationToken ct)
    {
        var existingConvention = (await systemTenantReadService.ListConventionsAsync(tenantId, ct)).FirstOrDefault();
        if (existingConvention is null)
            return null;

        var requestedAdmin = await tenantAwareUserService.FindTenantUserAsync(requestedAdminEmail, tenantId, ct);
        if (requestedAdmin is not null)
            return new ExistingProvisioning(existingConvention.Id, requestedAdmin.Id);

        var persons = await personRepository.ListByConventionIdAsync(new ConventionId(existingConvention.Id), ct);
        var existingAdmin = persons.FirstOrDefault(p => p.IsAdmin);
        if (existingAdmin is null)
            return new ExistingProvisioning(existingConvention.Id, null);

        var existingAdminUserId = await tenantAwareUserService.FindTenantUserIdByPersonAsync(
            tenantId,
            existingAdmin.Id,
            ct);

        return new ExistingProvisioning(existingConvention.Id, existingAdminUserId);
    }
}

public record CreateSystemTenantRequest(string Subdomain, string DisplayName);
public record TenantSignupRequest(string OrganizationName, string Subdomain, string ContactName, string ContactEmail);
public record TenantSignupResponse(Guid TenantId, Guid ConventionId, string ContactEmail, string Subdomain);
public record ProvisionTenantConventionRequest(
    string AdminName,
    string AdminEmail,
    string AdminPassword);
public record ProvisionTenantConventionResponse(Guid ConventionId, string? AdminUserId, bool AlreadyProvisioned);
public record TenantConventionDto(Guid Id, string Name, string Slug);
public record AddSystemAdministratorRequest(Guid PersonId);

sealed record ExistingProvisioning(Guid ConventionId, string? AdminUserId);
