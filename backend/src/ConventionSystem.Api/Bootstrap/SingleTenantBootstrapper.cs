using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.Bootstrap;

public static class SingleTenantBootstrapper
{
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
        var ambientTenantContext = sp.GetRequiredService<IAmbientTenantContext>();
        var tenantRepository = sp.GetRequiredService<ITenantRepository>();
        var db = sp.GetRequiredService<ConventionDbContext>();
        var conventionRepo = sp.GetRequiredService<IConventionRepository>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var tenantAwareUserService = sp.GetRequiredService<TenantAwareUserService>();

        var defaultTenantId = await EnsureDefaultTenantAsync(tenantRepository, multitenancy.DefaultSubdomain);
        await NormalizeExistingSingleTenantRowsAsync(db, defaultTenantId);

        using var tenantScope = ambientTenantContext.Use(defaultTenantId);

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

        var existingAdminUser = await tenantAwareUserService.FindTenantUserAsync(options.AdminEmail, defaultTenantId);
        if (existingAdminUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = options.AdminEmail,
                Email = options.AdminEmail,
                UserType = UserType.TenantUser,
                TenantId = defaultTenantId,
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
            if (existingAdminUser.TenantId != defaultTenantId)
            {
                existingAdminUser.TenantId = defaultTenantId;
                changed = true;
            }

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

    private static async Task<Guid> EnsureDefaultTenantAsync(
        ITenantRepository tenantRepository,
        string defaultSubdomain)
    {
        var normalizedSubdomain = string.IsNullOrWhiteSpace(defaultSubdomain)
            ? "default"
            : defaultSubdomain.Trim().ToLowerInvariant();

        var existingTenant = (await tenantRepository.ListAsync())
            .FirstOrDefault(tenant => tenant.Subdomain == normalizedSubdomain);

        if (existingTenant is not null)
            return existingTenant.Id;

        var tenant = new Tenant(TenantId.New(), normalizedSubdomain, "Default Tenant");
        await tenantRepository.AddAsync(tenant);
        await tenantRepository.SaveAsync();
        return tenant.Id.Value;
    }

    private static Task NormalizeExistingSingleTenantRowsAsync(ConventionDbContext db, Guid defaultTenantId)
    {
        return db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @TenantId uniqueidentifier = {defaultTenantId};
DECLARE @EmptyTenantId uniqueidentifier = '00000000-0000-0000-0000-000000000000';
DECLARE @Sql nvarchar(max) = N'';

SELECT @Sql = @Sql + N'
UPDATE ' + QUOTENAME(SCHEMA_NAME(t.[schema_id])) + N'.' + QUOTENAME(t.[name]) + N'
SET [tenant_id] = @TenantId
WHERE [tenant_id] = @EmptyTenantId;'
FROM sys.tables t
JOIN sys.columns c ON c.[object_id] = t.[object_id]
WHERE c.[name] = N'tenant_id';

IF @Sql <> N''
BEGIN
    EXEC sp_executesql
        @Sql,
        N'@TenantId uniqueidentifier, @EmptyTenantId uniqueidentifier',
        @TenantId,
        @EmptyTenantId;
END");
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
