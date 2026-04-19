using System.Security.Claims;
using ConventionSystem.Api.Auth;
using ConventionSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Api.Bootstrap;

public static class SystemAdminBootstrapper
{
    public static async Task SeedAsync(IServiceProvider appServices, IConfiguration configuration)
    {
        var logger = appServices.GetRequiredService<ILogger<Program>>();
        var options = configuration.GetSection("SystemAdminBootstrap").Get<SystemAdminBootstrapOptions>()
            ?? new SystemAdminBootstrapOptions();

        if (!options.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
            throw new InvalidOperationException("SystemAdminBootstrap är aktiverad men saknar e-post eller lösenord.");

        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var tenantAwareUserService = sp.GetRequiredService<TenantAwareUserService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var existingSystemAdmin = await tenantAwareUserService.FindSystemAdminAsync(options.Email);
        if (existingSystemAdmin is not null)
        {
            var claims = await userManager.GetClaimsAsync(existingSystemAdmin);
            var hasSystemClaim = claims.Any(c =>
                c.Type == AuthConstants.Claims.IsSystemAdmin &&
                c.Value == AuthConstants.Claims.IsSystemAdminTrue);

            if (!hasSystemClaim)
            {
                var claimResult = await userManager.AddClaimAsync(
                    existingSystemAdmin,
                    new Claim(AuthConstants.Claims.IsSystemAdmin, AuthConstants.Claims.IsSystemAdminTrue));

                if (!claimResult.Succeeded)
                {
                    var claimErrors = string.Join(" ", claimResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Kunde inte lägga till systemadmin-claim: {claimErrors}");
                }
            }

            logger.LogInformation("SystemAdminBootstrap: systemadmin finns redan ({Email}).", options.Email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            UserType = UserType.SystemAdmin,
            TenantId = null,
            PersonId = null,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, options.Password);
        if (!createResult.Succeeded)
        {
            var createErrors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Kunde inte skapa systemadmin: {createErrors}");
        }

        var addClaimResult = await userManager.AddClaimAsync(
            user,
            new Claim(AuthConstants.Claims.IsSystemAdmin, AuthConstants.Claims.IsSystemAdminTrue));

        if (!addClaimResult.Succeeded)
        {
            var claimErrors = string.Join(" ", addClaimResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Kunde inte sätta systemadmin-claim: {claimErrors}");
        }

        logger.LogInformation("SystemAdminBootstrap: skapade första systemadmin ({Email}).", options.Email);
    }
}

public sealed class SystemAdminBootstrapOptions
{
    public bool Enabled { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}