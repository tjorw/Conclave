using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Convention.Queries.ListPersons;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Application.Tenancy.Commands.CreateTenant;
using ConventionSystem.Application.Tenancy.Commands.RestoreTenant;
using ConventionSystem.Application.Tenancy.Commands.SuspendTenant;
using ConventionSystem.Application.Tenancy.Queries.ListTenants;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ConventionSystem.Api.Endpoints;

public static class SystemTenantEndpoints
{
    public static IEndpointRouteBuilder MapSystemTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/system/signup", async (
            TenantSignupRequest request,
            HttpContext httpContext,
            ISender sender,
            IPersonRepository personRepository,
            TenantAwareUserService tenantAwareUserService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
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
                return Results.Problem("Kunde inte skapa kontaktperson for tenanten.", statusCode: 422);

            var temporaryPassword = GenerateTemporaryPassword();
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
            var confirmLink = BuildSignupConfirmationLink(
                configuration,
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

        var group = app.MapGroup("/system/tenants")
            .RequireAuthorization(AuthConstants.Policies.IsSystemAdmin);

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
            ConventionDbContext db,
            CancellationToken ct) =>
        {
            var conventions = await db.Conventions
                .Where(c => EF.Property<Guid>(c, "TenantId") == tenantId)
                .OrderBy(c => c.Name)
                .Select(c => new TenantConventionDto(c.Id.Value, c.Name, c.Slug))
                .ToListAsync(ct);

            return Results.Ok(conventions);
        });

        group.MapGet("/{tenantId:guid}/conventions/{conventionId:guid}/persons", async (
            Guid tenantId,
            Guid conventionId,
            HttpContext httpContext,
            ISender sender,
            ConventionDbContext db,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var belongsToTenant = await db.Conventions
                .AnyAsync(c => c.Id == new ConventionId(conventionId)
                               && EF.Property<Guid>(c, "TenantId") == tenantId, ct);

            if (!belongsToTenant)
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
            ConventionDbContext db,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId), ct);
            if (convention is null)
                return Results.NotFound();

            var belongsToTenant = await db.Conventions
                .AnyAsync(c => c.Id == new ConventionId(conventionId)
                               && EF.Property<Guid>(c, "TenantId") == tenantId, ct);
            if (!belongsToTenant)
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
            ConventionDbContext db,
            CancellationToken ct) =>
        {
            httpContext.Items[TenantContextItemKeys.TenantId] = tenantId;

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId), ct);
            if (convention is null)
                return Results.NotFound();

            var belongsToTenant = await db.Conventions
                .AnyAsync(c => c.Id == new ConventionId(conventionId)
                               && EF.Property<Guid>(c, "TenantId") == tenantId, ct);
            if (!belongsToTenant)
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
                return Results.Problem("Kunde inte skapa admin-person for konventet.", statusCode: 422);

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

    private static string BuildSignupConfirmationLink(
        IConfiguration configuration,
        string email,
        Guid tenantId,
        string subdomain,
        string token)
    {
        var portalUrl = configuration["App:PortalUrl"] ?? "http://localhost:4202";

        return $"{portalUrl}/signup/confirm-email" +
               $"?email={Uri.EscapeDataString(email)}" +
               $"&token={Uri.EscapeDataString(token)}" +
               $"&tenantId={tenantId}" +
               $"&subdomain={Uri.EscapeDataString(subdomain)}";
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%&*?";
        var all = upper + lower + digits + special;

        var chars = new[]
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            special[RandomNumberGenerator.GetInt32(special.Length)]
        }.ToList();

        while (chars.Count < 14)
        {
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}

public record CreateSystemTenantRequest(string Subdomain, string DisplayName);
public record TenantSignupRequest(string OrganizationName, string Subdomain, string ContactName, string ContactEmail);
public record TenantSignupResponse(Guid TenantId, Guid ConventionId, string ContactEmail, string Subdomain);
public record ProvisionTenantConventionRequest(
    string ConventionName,
    string ConventionSlug,
    string AdminName,
    string AdminEmail,
    string AdminPassword);
public record TenantConventionDto(Guid Id, string Name, string Slug);
public record AddSystemAdministratorRequest(Guid PersonId);
