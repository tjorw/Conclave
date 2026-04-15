using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.DevData;

public static class DevDataSeeder
{
    private const string AdminEmail = "admin@demo.se";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider appServices, IConfiguration config)
    {
        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var conventionDb = sp.GetRequiredService<ConventionDbContext>();
        var logger = appServices.GetRequiredService<ILogger<Program>>();

        // Hoppa över seeding om konvention redan finns
        if (await conventionDb.Conventions.AnyAsync())
            return;

        logger.LogInformation("Seeder: skapar demo-data...");

        var conventionId = Guid.CreateVersion7();
        var sender = sp.GetRequiredService<ISender>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var conventionRepo = sp.GetRequiredService<IConventionRepository>();
        var editionRepo = sp.GetRequiredService<IEditionRepository>();

        // Konvention + admin-person via command (kräver ej auth)
        await sender.Send(new CreateConventionCommand(
            "Conclave Demo", "demo", "Admin Demo", AdminEmail, conventionId));

        var adminPerson = await personRepo.FindByEmailInConventionAsync(
            new ConventionId(conventionId), AdminEmail);

        // Identity-konto för admin
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            PersonId = adminPerson!.Id.Value,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, AdminPassword);

        // Hämta konventionen för att använda domänmodellen direkt (bypass MediatR/auth)
        var convention = await conventionRepo.GetSingleAsync();

        // Koordinatörer – skapas direkt via domänmodellen
        var staffCoord = convention!.CreatePerson("Saga Svensson", "saga@demo.se");
        var eventCoord = convention.CreatePerson("Erik Eriksson", "erik@demo.se");
        await personRepo.AddAndSaveAsync(staffCoord);
        await personRepo.AddAndSaveAsync(eventCoord);

        // Upplaga
        var period = new DatePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));
        var edition = convention.CreateEdition("Sommarcon 2026", period, staffCoord.Id, eventCoord.Id);
        await editionRepo.AddAndSaveAsync(edition);

        var storaSalen = edition.CreateVenue("Stora salen", "Huvudbyggnaden", "Konventionets huvudsal");
        var spelrummet = edition.CreateVenue("Spelrummet", "Annexet", null);

        // Funktionsområden och stationer
        var reception = edition.CreateStaffArea("Reception", adminPerson.Id, "Välkomnande och ackreditering");
        var nordingång = edition.CreateStation("Nordingång", reception.Id);
        var söderingång = edition.CreateStation("Söderingång", reception.Id);

        var spelsupport = edition.CreateStaffArea("Spelsupport", adminPerson.Id, "Hjälp med spel och evenemang");
        var salA = edition.CreateStation("Sal A", spelsupport.Id);
        var salB = edition.CreateStation("Sal B", spelsupport.Id);

        // Kategorier
        var rollspel = edition.CreateCategory("Rollspel", adminPerson.Id, "Pen & paper-rollspel");
        var brädspel = edition.CreateCategory("Brädspel", adminPerson.Id, "Moderna och klassiska brädspel");
        var lajv = edition.CreateCategory("Lajv", adminPerson.Id, "Levande rollspel");

        // Publicera upplagan
        edition.Publish(adminPerson.Id);
        await editionRepo.SaveAsync();

        logger.LogInformation(
            "Seeder: demo-konvention skapad (id={ConventionId}). Logga in med {Email} / {Password}",
            conventionId, AdminEmail, AdminPassword);
    }
}
