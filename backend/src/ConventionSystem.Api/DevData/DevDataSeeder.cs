using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Api.Bootstrap;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.ValueObjects;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using EventId = ConventionSystem.Domain.Event.Ids.EventId;

namespace ConventionSystem.Api.DevData;

public static class DevDataSeeder
{
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

        var bootstrapOptions = config.GetSection("SingleTenantBootstrap").Get<SingleTenantBootstrapOptions>()
            ?? new SingleTenantBootstrapOptions();

        await using var scope = appServices.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var logger = appServices.GetRequiredService<ILogger<Program>>();
        var sender = sp.GetRequiredService<ISender>();
        var ambientTenantContext = sp.GetRequiredService<IAmbientTenantContext>();
        var tenantRepository = sp.GetRequiredService<ITenantRepository>();
        var conventionRepo = sp.GetRequiredService<IConventionRepository>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var editionRepo = sp.GetRequiredService<IEditionRepository>();
        var eventRepo = sp.GetRequiredService<IEventRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        logger.LogInformation("Seeder: ensuring demo data...");

        var defaultTenantId = await GetDefaultTenantIdAsync(tenantRepository, multitenancy.DefaultSubdomain);
        using var tenantScope = ambientTenantContext.Use(defaultTenantId);

        var convention = await conventionRepo.GetSingleAsync();
        if (convention is null)
        {
            var conventionId = Guid.CreateVersion7();
            await sender.Send(new CreateConventionCommand(
                "Conclave Demo",
                "demo",
                bootstrapOptions.AdminName,
                bootstrapOptions.AdminEmail,
                conventionId));

            convention = await conventionRepo.GetByIdAsync(new ConventionId(conventionId))
                ?? throw new InvalidOperationException("Seeder: convention was created but could not be loaded.");
        }

        var adminPerson = await personRepo.FindByEmailInConventionAsync(convention.Id, bootstrapOptions.AdminEmail);
        if (adminPerson is null)
        {
            adminPerson = convention.RegisterPerson(bootstrapOptions.AdminName, bootstrapOptions.AdminEmail);
            convention.AddAdministrator(adminPerson.Id, adminPerson.Id);
            await personRepo.AddAndSaveAsync(adminPerson);
        }
        else if (!convention.IsAdministrator(adminPerson.Id))
        {
            convention.AddAdministrator(adminPerson.Id, adminPerson.Id);
            await conventionRepo.SaveAsync();
        }

        await EnsureAdminUserAsync(userManager, bootstrapOptions, defaultTenantId, adminPerson.Id.Value);

        var staffCoord = await EnsurePersonAsync(personRepo, convention, "Saga Svensson", "saga@demo.se");
        var eventCoord = await EnsurePersonAsync(personRepo, convention, "Erik Eriksson", "erik@demo.se");

        var edition = await EnsureEditionAsync(
            editionRepo,
            conventionRepo,
            convention,
            staffCoord.Id,
            eventCoord.Id,
            adminPerson.Id);
        await EnsureProgramAsync(eventRepo, editionRepo, edition.Id, eventCoord.Id, adminPerson.Id);

        logger.LogInformation(
            "Seeder: demo data ensured for convention {ConventionId}. Login with {Email} / {Password}",
            convention.Id.Value,
            bootstrapOptions.AdminEmail,
            bootstrapOptions.AdminPassword);
    }

    private static async Task EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        SingleTenantBootstrapOptions bootstrapOptions,
        Guid tenantId,
        Guid adminPersonId)
    {
        var existingUser = await userManager.FindByEmailAsync(bootstrapOptions.AdminEmail);
        if (existingUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = bootstrapOptions.AdminEmail,
                Email = bootstrapOptions.AdminEmail,
                UserType = UserType.TenantUser,
                TenantId = tenantId,
                PersonId = adminPersonId,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, bootstrapOptions.AdminPassword);
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
        if (existingUser.TenantId != tenantId)
        {
            existingUser.TenantId = tenantId;
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

    private static async Task<Guid> GetDefaultTenantIdAsync(
        ITenantRepository tenantRepository,
        string defaultSubdomain)
    {
        var normalizedSubdomain = string.IsNullOrWhiteSpace(defaultSubdomain)
            ? "default"
            : defaultSubdomain.Trim().ToLowerInvariant();

        var tenant = (await tenantRepository.ListAsync())
            .FirstOrDefault(item => item.Subdomain == normalizedSubdomain);

        if (tenant is null)
            throw new InvalidOperationException($"Seeder: default tenant '{normalizedSubdomain}' does not exist.");

        return tenant.Id;
    }

    private static async Task<Domain.Convention.Entities.Person> EnsurePersonAsync(
        IPersonRepository personRepo,
        Domain.Convention.Aggregates.Convention convention,
        string name,
        string email)
    {
        var existing = await personRepo.FindByEmailInConventionAsync(convention.Id, email);
        if (existing is not null)
            return existing;

        var person = convention.CreatePerson(name, email);
        await personRepo.AddAndSaveAsync(person);
        return person;
    }

    private static async Task<Domain.Convention.Aggregates.Edition> EnsureEditionAsync(
        IEditionRepository editionRepo,
        IConventionRepository conventionRepo,
        Domain.Convention.Aggregates.Convention convention,
        PersonId staffCoordinatorId,
        PersonId eventCoordinatorId,
        PersonId adminPersonId)
    {
        var editions = await editionRepo.ListByConventionIdAsync(convention.Id);
        if (editions.Count > 0)
        {
            var existingEditionId = new EditionId(editions[0].Id);
            if (convention.ActiveEditionId != existingEditionId)
            {
                convention.SetActiveEdition(existingEditionId);
                await conventionRepo.SaveAsync();
            }

            return await editionRepo.GetByIdWithCategoriesAndVenuesAsync(existingEditionId)
                ?? throw new InvalidOperationException("Seeder: edition summary existed but edition could not be loaded.");
        }

        var period = new DatePeriod(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3));
        var edition = convention.CreateEdition("Sommarcon 2026", period, staffCoordinatorId, eventCoordinatorId);
        await editionRepo.AddAndSaveAsync(edition);

        edition.CreateVenue("Stora salen", "Huvudbyggnaden", "Konventionets huvudsal");
        edition.CreateVenue("Spelrummet", "Annexet", null);

        var reception = edition.CreateStaffArea("Reception", adminPersonId, "Valkomnande och ackreditering");
        edition.CreateStation("Nordingång", reception.Id);
        edition.CreateStation("Söderingång", reception.Id);

        var gameSupport = edition.CreateStaffArea("Spelsupport", adminPersonId, "Hjalp med spel och evenemang");
        edition.CreateStation("Sal A", gameSupport.Id);
        edition.CreateStation("Sal B", gameSupport.Id);

        edition.CreateCategory("Rollspel", adminPersonId, "Pen & paper-rollspel");
        edition.CreateCategory("Brädspel", adminPersonId, "Moderna och klassiska brädspel");
        edition.CreateCategory("Lajv", adminPersonId, "Levande rollspel");

        edition.Publish(adminPersonId);
        convention.SetActiveEdition(edition.Id);
        await editionRepo.SaveAsync();

        return edition;
    }

    private static async Task EnsureProgramAsync(
        IEventRepository eventRepo,
        IEditionRepository editionRepo,
        EditionId editionId,
        PersonId leadOrganiserId,
        PersonId approvedById)
    {
        var existingEvents = await eventRepo.ListByEditionIdAsync(editionId);
        if (existingEvents.Any(e => e.Status == nameof(EventStatus.Published)))
            return;

        var edition = await editionRepo.GetByIdWithCategoriesAndVenuesAsync(editionId)
            ?? throw new InvalidOperationException("Seeder: edition could not be loaded for program data.");

        if (edition.Categories.Count == 0 || edition.Venues.Count == 0)
            throw new InvalidOperationException("Seeder: edition needs categories and venues before program data can be created.");

        var rollspel = edition.Categories.FirstOrDefault(c => c.Name == "Rollspel") ?? edition.Categories[0];
        var bradspel = edition.Categories.FirstOrDefault(c => c.Name == "Bradspel") ?? edition.Categories[0];
        var lajv = edition.Categories.FirstOrDefault(c => c.Name == "Lajv") ?? edition.Categories[0];
        var mainVenue = edition.Venues.FirstOrDefault(v => v.Name == "Stora salen") ?? edition.Venues[0];
        var sideVenue = edition.Venues.FirstOrDefault(v => v.Name == "Spelrummet") ?? edition.Venues[0];

        var eventDate = edition.Period.StartDate.ToDateTime(TimeOnly.MinValue);

        await AddPublishedEventAsync(
            eventRepo,
            edition.Id,
            rollspel.Id,
            leadOrganiserId,
            approvedById,
            mainVenue.Id,
            "Drakar över Dimskogen",
            "Ett introduktionsvänligt fantasyäventyr med mysterier, förhandlingar och ett par dåliga idéer som kan bli hjälte­dåd.",
            eventDate.AddHours(10),
            eventDate.AddHours(12),
            6,
            RegistrationType.PreRegistration);

        await AddPublishedEventAsync(
            eventRepo,
            edition.Id,
            bradspel.Id,
            leadOrganiserId,
            approvedById,
            sideVenue.Id,
            "Terraforming Mars: nybörjarbord",
            "Lugn genomgång och spel för dig som vill prova ett tyngre strategispel utan stress.",
            eventDate.AddHours(13),
            eventDate.AddHours(15),
            4,
            RegistrationType.Combined);

        await AddPublishedEventAsync(
            eventRepo,
            edition.Id,
            lajv.Id,
            leadOrganiserId,
            approvedById,
            mainVenue.Id,
            "Intrigverkstad för förstagångs-lajvare",
            "Kort workshop där vi bygger roller, relationer och scener tillsammans.",
            eventDate.AddDays(1).AddHours(11),
            eventDate.AddDays(1).AddHours(12),
            12,
            RegistrationType.DropIn,
            "Kom fem minuter innan start.");
    }

    private static async Task AddPublishedEventAsync(
        IEventRepository eventRepo,
        EditionId editionId,
        CategoryId categoryId,
        PersonId leadOrganiserId,
        PersonId approvedById,
        VenueId venueId,
        string title,
        string description,
        DateTime start,
        DateTime end,
        int maxSeats,
        RegistrationType registrationType,
        string? dropInRules = null)
    {
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), editionId, categoryId, leadOrganiserId);
        ev.EditTitle(title);
        ev.EditDescription(description);
        ev.SetRegistrationType(registrationType, dropInRules);
        ev.SubmitForReview();
        ev.Approve(approvedById);
        ev.CreateSession(venueId, new TimeSlot(start, end), maxSeats, StartType.FixedTime);

        await eventRepo.AddAndSaveAsync(ev);
    }
}
