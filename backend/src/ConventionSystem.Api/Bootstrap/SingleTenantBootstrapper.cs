using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Api.Bootstrap;

public static class SingleTenantBootstrapper
{
    private static readonly Guid SingleTenantId = Guid.Empty;

    public static async Task SeedAsync(IServiceProvider appServices, IConfiguration configuration)
    {
        var multitenancy = configuration.GetSection(MultitenancyOptions.SectionName).Get<MultitenancyOptions>()
            ?? new MultitenancyOptions();

        if (multitenancy.Enabled)
            return;

        var options = configuration.GetSection("SingleTenantBootstrap").Get<SingleTenantBootstrapOptions>()
            ?? new SingleTenantBootstrapOptions();

        if (!options.Enabled)
            return;

        Validate(options);

        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = appServices.GetRequiredService<ILogger<Program>>();
        var sender = sp.GetRequiredService<ISender>();
        var conventionRepo = sp.GetRequiredService<IConventionRepository>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var tenantAwareUserService = sp.GetRequiredService<TenantAwareUserService>();

        var convention = await conventionRepo.GetSingleAsync();
        if (convention is null)
        {
            var conventionId = await sender.Send(new CreateConventionCommand(
                options.ConventionName,
                options.ConventionSlug,
                options.AdminName,
                options.AdminEmail));

            convention = await conventionRepo.GetByIdAsync(new ConventionId(conventionId))
                ?? throw new InvalidOperationException("SingleTenantBootstrap: convention was created but could not be loaded.");
        }

        var adminPerson = await personRepo.FindByEmailInConventionAsync(convention.Id, options.AdminEmail);
        if (adminPerson is null)
        {
            adminPerson = convention.RegisterPerson(options.AdminName, options.AdminEmail);

            if (!convention.IsAdministrator(adminPerson.Id))
                convention.AddAdministrator(adminPerson.Id, adminPerson.Id);

            await personRepo.AddAndSaveAsync(adminPerson);
        }
        else if (!convention.IsAdministrator(adminPerson.Id))
        {
            convention.AddAdministrator(adminPerson.Id, adminPerson.Id);
            await conventionRepo.SaveAsync();
        }

        var existingAdminUser = await tenantAwareUserService.FindTenantUserAsync(options.AdminEmail, SingleTenantId);
        if (existingAdminUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = options.AdminEmail,
                Email = options.AdminEmail,
                UserType = UserType.TenantUser,
                TenantId = SingleTenantId,
                PersonId = adminPerson.Id.Value,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, options.AdminPassword);
            if (!createResult.Succeeded)
            {
                var createErrors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"SingleTenantBootstrap: could not create admin user: {createErrors}");
            }
        }
        else
        {
            var changed = false;
            if (existingAdminUser.PersonId != adminPerson.Id.Value)
            {
                existingAdminUser.PersonId = adminPerson.Id.Value;
                changed = true;
            }

            if (!existingAdminUser.EmailConfirmed)
            {
                existingAdminUser.EmailConfirmed = true;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(existingAdminUser);
                if (!updateResult.Succeeded)
                {
                    var updateErrors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"SingleTenantBootstrap: could not update admin user: {updateErrors}");
                }
            }
        }

        logger.LogInformation(
            "SingleTenantBootstrap: ensured convention ({ConventionSlug}) and admin ({Email}).",
            convention.Slug,
            options.AdminEmail);
    }

    private static void Validate(SingleTenantBootstrapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConventionName))
            throw new InvalidOperationException("SingleTenantBootstrap is enabled but ConventionName is missing.");
        if (string.IsNullOrWhiteSpace(options.ConventionSlug))
            throw new InvalidOperationException("SingleTenantBootstrap is enabled but ConventionSlug is missing.");
        if (string.IsNullOrWhiteSpace(options.AdminName))
            throw new InvalidOperationException("SingleTenantBootstrap is enabled but AdminName is missing.");
        if (string.IsNullOrWhiteSpace(options.AdminEmail))
            throw new InvalidOperationException("SingleTenantBootstrap is enabled but AdminEmail is missing.");
        if (string.IsNullOrWhiteSpace(options.AdminPassword))
            throw new InvalidOperationException("SingleTenantBootstrap is enabled but AdminPassword is missing.");
    }
}

public sealed class SingleTenantBootstrapOptions
{
    public bool Enabled { get; init; }
    public string ConventionName { get; init; } = "Conclave Local";
    public string ConventionSlug { get; init; } = "local";
    public string AdminName { get; init; } = "Local Admin";
    public string AdminEmail { get; init; } = "admin@local.dev";
    public string AdminPassword { get; init; } = "Admin123!";
}
