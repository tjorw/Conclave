using ConventionSystem.Api.Auth;
using ConventionSystem.Api.Helpers;
using ConventionSystem.Api.Services;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroups groups)
    {
        groups.Anonymous.MapPost("/auth/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantContext tenantContext,
            IOptions<MultitenancyOptions> multitenancyOptions,
            IConventionRepository conventionRepo,
            IEditionRepository editionRepo,
            IPersonRepository personRepo,
            IJwtTokenIssuer jwtTokenIssuer,
            CancellationToken ct) =>
        {
            ApplicationUser? user = multitenancyOptions.Value.Enabled
                ? await tenantAwareUserService.FindTenantUserAsync(request.Email, tenantContext.TenantId, ct)
                : await userManager.FindByEmailAsync(request.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            if (!user.EmailConfirmed)
                return Results.Problem(
                    title: "E-postadressen är inte bekräftad.",
                    detail: "Kontrollera din inkorg och klicka på bekräftelselänken.",
                    statusCode: 403);

            var identityClaims = await userManager.GetClaimsAsync(user);
            var isSystemAdmin = identityClaims.Any(c =>
                c.Type == AuthConstants.Claims.IsSystemAdmin &&
                c.Value == AuthConstants.Claims.IsSystemAdminTrue);

            var convention = await conventionRepo.GetSingleAsync(ct);
            if (convention is null)
                return Results.Problem("Konventet är inte konfigurerat.");

            var conventionId = convention.Id;

            Guid personId;
            if (user.PersonId.HasValue)
            {
                personId = user.PersonId.Value;

                var linkedPerson = await personRepo.GetByIdAsync(new PersonId(personId), ct);
                if (linkedPerson is not null && linkedPerson.ConventionId != convention.Id)
                {
                    convention = await conventionRepo.GetByIdAsync(linkedPerson.ConventionId, ct)
                        ?? convention;
                }
            }
            else
            {
                // Första inloggningen – identifiera eller skapa person
                var existingPerson = await personRepo.FindByEmailInConventionAsync(conventionId, request.Email, ct);

                if (existingPerson is not null)
                {
                    // Person finns redan (t.ex. skapad av admin) – koppla identitetskontot
                    personId = existingPerson.Id.Value;
                }
                else
                {
                    // Skapa nytt personkonto – namn saknas i detta flöde, sätts via profilvyn
                    var person = convention.RegisterPerson(string.Empty, request.Email);
                    await personRepo.AddAndSaveAsync(person, ct);
                    personId = person.Id.Value;
                }

                user.PersonId = personId;
                await userManager.UpdateAsync(user);
            }

            var personIdTyped = new PersonId(personId);
            var isAdmin = convention.IsAdministrator(personIdTyped);

            var isReception = false;
            if (convention.ActiveEditionId is { } activeEditionId)
            {
                var activeEdition = await editionRepo.GetByIdWithReceptionStaffAsync(activeEditionId, ct);
                isReception = isAdmin || (activeEdition?.IsReceptionStaff(personIdTyped) ?? false);
            }

            var token = jwtTokenIssuer.Issue(
                personId,
                isAdmin,
                isReception,
                isSystemAdmin,
                AuthConstants.Claims.UserTypeTenantUser,
                multitenancyOptions.Value.Enabled ? tenantContext.TenantId : user.TenantId);
            return Results.Ok(new { token });
        });

        groups.Anonymous.MapPost("/system/auth/login", async (
            HttpContext httpContext,
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            IJwtTokenIssuer jwtTokenIssuer,
            CancellationToken ct) =>
        {
            var subdomain = HostNameHelpers.TryExtractSubdomain(httpContext.Request.Host.Host);
            if (!string.IsNullOrWhiteSpace(subdomain)
                && !subdomain.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound();
            }

            var user = await tenantAwareUserService.FindSystemAdminAsync(request.Email, ct);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            if (!user.EmailConfirmed)
                return Results.Problem(
                    title: "E-postadressen är inte bekräftad.",
                    detail: "Kontrollera din inkorg och klicka på bekräftelselänken.",
                    statusCode: 403);

            var token = jwtTokenIssuer.Issue(
                user.PersonId,
                isAdmin: false,
                isReception: false,
                isSystemAdmin: true,
                AuthConstants.Claims.UserTypeSystemAdmin,
                tenantId: null);
            return Results.Ok(new { token });
        });

        groups.Anonymous.MapPost("/auth/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantContext tenantContext,
            IOptions<MultitenancyOptions> multitenancyOptions,
            IConventionRepository conventionRepo,
            IPersonRepository personRepo,
            IEmailService emailService,
            IAuthLinkBuilder authLinkBuilder,
            CancellationToken ct) =>
        {
            Guid? tenantId = multitenancyOptions.Value.Enabled ? tenantContext.TenantId : null;

            if (tenantId.HasValue)
            {
                var existingUser = await tenantAwareUserService.FindTenantUserAsync(request.Email, tenantId.Value, ct);
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
            }

            Domain.Convention.Aggregates.Convention? convention = null;
            if (multitenancyOptions.Value.Enabled)
            {
                convention = await conventionRepo.GetSingleAsync(ct);
                if (convention is null)
                    return Results.Problem("Konventet är inte konfigurerat.", statusCode: 422);
            }

            var user = new ApplicationUser
            {
                UserName = tenantId.HasValue
                    ? $"{tenantId.Value:N}_{request.Email}"
                    : request.Email,
                Email = request.Email,
                UserType = UserType.TenantUser,
                TenantId = tenantId
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
                {
                    if (tenantId.HasValue)
                    {
                        return Results.Problem(
                            title: "E-postadressen används redan.",
                            statusCode: 422,
                            extensions: new Dictionary<string, object?>
                            {
                                ["errorCode"] = "email_already_exists"
                            });
                    }

                    return Results.Problem("E-postadressen används redan.", statusCode: 400);
                }

                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return Results.Problem(errors, statusCode: 400);
            }

            if (convention is not null)
            {
                var person = convention.RegisterPerson(string.Empty, request.Email);
                await personRepo.AddAndSaveAsync(person, ct);
                user.PersonId = person.Id.Value;
                await userManager.UpdateAsync(user);
            }

            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = authLinkBuilder.BuildEmailConfirmationLink(request.Email, emailToken, tenantId);

            await emailService.SendEmailConfirmationAsync(request.Email, string.Empty, confirmLink, ct);

            return Results.Ok();
        });

        groups.Anonymous.MapPost("/auth/confirm-email", async (
            ConfirmEmailRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantRepository tenantRepository,
            CancellationToken ct) =>
        {
            var user = request.TenantId.HasValue
                ? await tenantAwareUserService.FindTenantUserAsync(request.Email, request.TenantId.Value, ct)
                : await userManager.FindByEmailAsync(request.Email);

            if (user is null)
                return Results.Problem("Ogiltig länk.", statusCode: 400);

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
                return Results.Problem("Länken är ogiltig eller har gått ut.", statusCode: 400);

            if (user.UserType == UserType.TenantUser && user.TenantId.HasValue)
            {
                var claims = await userManager.GetClaimsAsync(user);
                var shouldActivateTenant = claims.Any(c =>
                    c.Type == "activates_tenant" &&
                    c.Value == "true");

                if (shouldActivateTenant)
                {
                    var tenant = await tenantRepository.GetByIdAsync(new TenantId(user.TenantId.Value), ct);
                    if (tenant is not null && tenant.Status == TenantStatus.Suspended)
                    {
                        tenant.Restore();
                        await tenantRepository.SaveAsync(ct);
                    }

                    var activationClaim = claims.First(c => c.Type == "activates_tenant" && c.Value == "true");
                    await userManager.RemoveClaimAsync(user, activationClaim);
                }
            }

            return Results.Ok();
        });

        groups.Anonymous.MapPost("/auth/resend-confirmation", async (
            ResendConfirmationRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantContext tenantContext,
            IOptions<MultitenancyOptions> multitenancyOptions,
            IPersonRepository personRepo,
            IEmailService emailService,
            IAuthLinkBuilder authLinkBuilder,
            CancellationToken ct) =>
        {
            Guid? tenantId = multitenancyOptions.Value.Enabled ? tenantContext.TenantId : null;

            ApplicationUser? user = tenantId.HasValue
                ? await tenantAwareUserService.FindTenantUserAsync(request.Email, tenantId.Value, ct)
                : await userManager.FindByEmailAsync(request.Email);

            if (user is not null && !user.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = authLinkBuilder.BuildEmailConfirmationLink(request.Email, token, tenantId);

                var name = user.PersonId.HasValue
                    ? (await personRepo.GetByIdAsync(new PersonId(user.PersonId.Value), ct))?.Name ?? string.Empty
                    : string.Empty;

                await emailService.SendResendConfirmationAsync(request.Email, name, confirmLink, ct);
            }

            return Results.Ok();
        });

        groups.Anonymous.MapPost("/auth/forgot-password", async (
            ForgotPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantContext tenantContext,
            IOptions<MultitenancyOptions> multitenancyOptions,
            IPersonRepository personRepo,
            IEmailService emailService,
            IAuthLinkBuilder authLinkBuilder,
            CancellationToken ct) =>
        {
            Guid? tenantId = multitenancyOptions.Value.Enabled ? tenantContext.TenantId : null;

            ApplicationUser? user = tenantId.HasValue
                ? await tenantAwareUserService.FindTenantUserAsync(request.Email, tenantId.Value, ct)
                : await userManager.FindByEmailAsync(request.Email);

            if (user is not null && user.EmailConfirmed)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = authLinkBuilder.BuildPasswordResetLink(request.Email, token, tenantId);

                var name = user.PersonId.HasValue
                    ? (await personRepo.GetByIdAsync(new PersonId(user.PersonId.Value), ct))?.Name ?? string.Empty
                    : string.Empty;

                await emailService.SendPasswordResetAsync(request.Email, name, resetLink, ct);
            }

            return Results.Ok();
        });

        groups.Anonymous.MapPost("/auth/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            TenantAwareUserService tenantAwareUserService,
            ITenantContext tenantContext,
            IOptions<MultitenancyOptions> multitenancyOptions,
            CancellationToken ct) =>
        {
            Guid? tenantId = multitenancyOptions.Value.Enabled ? tenantContext.TenantId : null;

            var user = tenantId.HasValue
                ? await tenantAwareUserService.FindTenantUserAsync(request.Email, tenantId.Value, ct)
                : await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Problem("Ogiltig återställningslänk.", statusCode: 400);

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
                return Results.Problem("Länken är ogiltig eller har gått ut.", statusCode: 400);

            return Results.Ok();
        });

        groups.Authenticated.MapPut("/auth/password", async (
            ChangePasswordRequest request,
            ICurrentUser currentUser,
            UserManager<ApplicationUser> userManager,
            IPersonRepository personRepo,
            IEmailService emailService,
            CancellationToken ct) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.PersonId == currentUser.PersonId.Value, ct);
            if (user is null)
                return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return Results.Problem(errors, statusCode: 400);
            }

            var person = await personRepo.GetByIdAsync(currentUser.PersonId, ct);
            await emailService.SendPasswordChangedAsync(user.Email!, person?.Name ?? string.Empty, ct);
            return Results.NoContent();
        });
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password);
public record ConfirmEmailRequest(string Email, string Token, Guid? TenantId = null);
public record ResendConfirmationRequest(string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
