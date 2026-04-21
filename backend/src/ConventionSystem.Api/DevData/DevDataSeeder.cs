using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Api.DevData;

public static class DevDataSeeder
{
    private const string AdminEmail = "admin@demo.se";
    private const string AdminPassword = "Admin123!";
    private const string AdminName = "Admin Demo";

    public static async Task SeedAsync(IServiceProvider appServices, IConfiguration config)
    {
        var multitenancy = config.GetSection(MultitenancyOptions.SectionName).Get<MultitenancyOptions>()
            ?? new MultitenancyOptions();

        if (multitenancy.Enabled)
        {
            var skipLogger = appServices.GetRequiredService<ILogger<Program>>();
            skipLogger.LogInformation("Seeder: skipping demo data because multitenancy is enabled.");
            return;
        }

        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = appServices.GetRequiredService<ILogger<Program>>();
        var sender = sp.GetRequiredService<ISender>();
        var conventionRepo = sp.GetRequiredService<IConventionRepository>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var editionRepo = sp.GetRequiredService<IEditionRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        logger.LogInformation("Seeder: ensuring demo data...");

        var convention = await conventionRepo.GetSingleAsync();
        if (convention is null)
        {
            var conventionId = Guid.CreateVersion7();
            await sender.Send(new CreateConventionCommand(
                "Conclave Demo",
                "demo",
                AdminName,
                AdminEmail,
                conventionId));

            convention = await conventionRepo.GetByIdAsync(new ConventionId(conventionId))
                ?? throw new InvalidOperationException("Seeder: convention was created but could not be loaded.");
        }

        var adminPerson = await personRepo.FindByEmailInConventionAsync(convention.Id, AdminEmail);
        if (adminPerson is null)
        {
            adminPerson = convention.RegisterPerson(AdminName, AdminEmail);
            convention.AddAdministrator(adminPerson.Id, adminPerson.Id);
            await personRepo.AddAndSaveAsync(adminPerson);
        }
        else if (!convention.IsAdministrator(adminPerson.Id))
        {
            convention.AddAdministrator(adminPerson.Id, adminPerson.Id);
            await conventionRepo.SaveAsync();
        }

        await EnsureAdminUserAsync(userManager, adminPerson.Id.Value);

        var editions = await editionRepo.ListByConventionIdAsync(convention.Id);
        if (editions.Count > 0)
        {
            logger.LogInformation("Seeder: convention already has editions, skipping demo structure.");
            return;
        }

        var staffCoord = convention.CreatePerson("Saga Svensson", "saga@demo.se");
        var eventCoord = convention.CreatePerson("Erik Eriksson", "erik@demo.se");
        await personRepo.AddAndSaveAsync(staffCoord);
        await personRepo.AddAndSaveAsync(eventCoord);

        var period = new DatePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));
        var edition = convention.CreateEdition("Sommarcon 2026", period, staffCoord.Id, eventCoord.Id);
        await editionRepo.AddAndSaveAsync(edition);

        edition.CreateVenue("Stora salen", "Huvudbyggnaden", "Konventionets huvudsal");
        edition.CreateVenue("Spelrummet", "Annexet", null);

        var reception = edition.CreateStaffArea("Reception", adminPerson.Id, "Valkomnande och ackreditering");
        edition.CreateStation("Nordingang", reception.Id);
        edition.CreateStation("Soderingang", reception.Id);

        var gameSupport = edition.CreateStaffArea("Spelsupport", adminPerson.Id, "Hjalp med spel och evenemang");
        edition.CreateStation("Sal A", gameSupport.Id);
        edition.CreateStation("Sal B", gameSupport.Id);

        edition.CreateCategory("Rollspel", adminPerson.Id, "Pen & paper-rollspel");
        edition.CreateCategory("Bradspel", adminPerson.Id, "Moderna och klassiska bradspel");
        edition.CreateCategory("Lajv", adminPerson.Id, "Levande rollspel");

        edition.Publish(adminPerson.Id);
        await editionRepo.SaveAsync();

        logger.LogInformation(
            "Seeder: demo structure created for convention {ConventionId}. Login with {Email} / {Password}",
            convention.Id.Value,
            AdminEmail,
            AdminPassword);
    }

    private static async Task EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        Guid adminPersonId)
    {
        var existingUser = await userManager.FindByEmailAsync(AdminEmail);
        if (existingUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                UserType = UserType.TenantUser,
                TenantId = Guid.Empty,
                PersonId = adminPersonId,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, AdminPassword);
            if (!createResult.Succeeded)
            {
                var createErrors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Seeder: could not create demo admin user: {createErrors}");
            }

            return;
        }

        if (existingUser.UserType != UserType.TenantUser)
            return;

        var changed = false;
        if (existingUser.TenantId != Guid.Empty)
        {
            existingUser.TenantId = Guid.Empty;
            changed = true;
        }

        if (existingUser.PersonId != adminPersonId)
        {
            existingUser.PersonId = adminPersonId;
            changed = true;
        }

        if (!existingUser.EmailConfirmed)
        {
            existingUser.EmailConfirmed = true;
            changed = true;
        }

        if (!changed)
            return;

        var updateResult = await userManager.UpdateAsync(existingUser);
        if (!updateResult.Succeeded)
        {
            var updateErrors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Seeder: could not update demo admin user: {updateErrors}");
        }
    }
}
