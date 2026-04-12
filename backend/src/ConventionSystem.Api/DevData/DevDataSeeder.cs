using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.AddAdministrator;
using ConventionSystem.Application.Convention.Commands.CreateCategory;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.CreatePerson;
using ConventionSystem.Application.Convention.Commands.CreateStaffArea;
using ConventionSystem.Application.Convention.Commands.CreateStation;
using ConventionSystem.Application.Convention.Commands.CreateVenue;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Infrastructure.System;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.DevData;

public static class DevDataSeeder
{
    private const string AdminEmail = "admin@demo.se";
    private const string AdminPassword = "Admin123!";
    private const string ConventionSlug = "demo";

    public static async Task SeedAsync(IServiceProvider appServices, IConfiguration config)
    {
        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var systemDb = sp.GetRequiredService<SystemDbContext>();
        var logger = appServices.GetRequiredService<ILogger<Program>>();

        // Migrera alltid – idempotent och nödvändigt vid nya migrationer
        var connStr = DeriveConnectionString(config, "ConventionDemo");
        var dbOptions = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseSqlServer(connStr).Options;
        await using (var db = new ConventionDbContext(dbOptions))
            await db.Database.MigrateAsync();

        if (await systemDb.Tenants.AnyAsync(t => t.Slug == ConventionSlug))
            return;

        logger.LogInformation("Seeder: skapar demo-data...");

        var conventionId = Guid.CreateVersion7();

        // Lös tenant-kontexten för scopet så att ConventionDbContext pekar rätt
        var tenantContext = sp.GetRequiredService<TenantContext>();
        tenantContext.Resolve(conventionId, connStr);

        var sender = sp.GetRequiredService<ISender>();

        // Konvention + admin-person
        await sender.Send(new CreateConventionCommand(
            "Conclave Demo", ConventionSlug, "Admin Demo", AdminEmail, conventionId));

        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var adminPerson = await personRepo.FindByEmailInConventionAsync(
            new ConventionId(conventionId), AdminEmail);

        // Identity-konto för admin
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var identityDb = sp.GetRequiredService<ApplicationIdentityDbContext>();
        var user = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail };
        await userManager.CreateAsync(user, AdminPassword);
        await identityDb.ConventionUserLinks.AddAsync(
            ConventionUserLink.Create(user.Id, conventionId, adminPerson!.Id.Value));
        await identityDb.SaveChangesAsync();

        // Tenant-post
        await systemDb.Tenants.AddAsync(Tenant.Create(conventionId, ConventionSlug, connStr, null));
        await systemDb.SaveChangesAsync();

        // Koordinatörer
        var staffCoordId = await sender.Send(new CreatePersonCommand(
            conventionId, "Saga Svensson", "saga@demo.se", null));
        var eventCoordId = await sender.Send(new CreatePersonCommand(
            conventionId, "Erik Eriksson", "erik@demo.se", null));

        // Upplaga
        var editionId = await sender.Send(new CreateEditionCommand(
            conventionId,
            "Sommarcon 2026",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 3),
            staffCoordId,
            eventCoordId));

        // Lokaler
        await sender.Send(new CreateVenueCommand(editionId, "Stora salen", "Huvudbyggnaden", "Konventionets huvudsal"));
        await sender.Send(new CreateVenueCommand(editionId, "Spelrummet", "Annexet", null));

        // Funktionsområden och stationer
        var receptionId = await sender.Send(new CreateStaffAreaCommand(
            editionId, "Reception", "Välkomnande och ackreditering", adminPerson.Id.Value));
        await sender.Send(new CreateStationCommand(editionId, "Nordingång", null, receptionId));
        await sender.Send(new CreateStationCommand(editionId, "Söderingång", null, receptionId));

        var spelsupportId = await sender.Send(new CreateStaffAreaCommand(
            editionId, "Spelsupport", "Hjälp med spel och evenemang", adminPerson.Id.Value));
        await sender.Send(new CreateStationCommand(editionId, "Sal A", null, spelsupportId));
        await sender.Send(new CreateStationCommand(editionId, "Sal B", null, spelsupportId));

        // Kategorier
        await sender.Send(new CreateCategoryCommand(
            editionId, "Rollspel", "Pen & paper-rollspel", adminPerson.Id.Value));
        await sender.Send(new CreateCategoryCommand(
            editionId, "Brädspel", "Moderna och klassiska brädspel", adminPerson.Id.Value));
        await sender.Send(new CreateCategoryCommand(
            editionId, "Lajv", "Levande rollspel", adminPerson.Id.Value));

        // Publicera upplagan
        await sender.Send(new PublishEditionCommand(editionId));

        logger.LogInformation(
            "Seeder: demo-konvention skapad (id={ConventionId}). Logga in med {Email} / {Password}",
            conventionId, AdminEmail, AdminPassword);
    }

    private static string DeriveConnectionString(IConfiguration config, string database)
    {
        var systemConnStr = config.GetConnectionString("SystemDb")
            ?? throw new InvalidOperationException("ConnectionStrings:SystemDb saknas.");
        return new SqlConnectionStringBuilder(systemConnStr) { InitialCatalog = database }.ConnectionString;
    }
}
