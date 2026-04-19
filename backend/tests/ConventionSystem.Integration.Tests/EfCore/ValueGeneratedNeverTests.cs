using System.Net;
using System.Net.Http.Headers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Integration.Tests.EfCore;

/// <summary>
/// Bevisar att ValueGeneratedNever() på applikationsgenererade ID:n gör att EF Core
/// korrekt kan spara nya barnentiteter som lagts till via navigationssamlingar –
/// utan att MarkAsAdded() behöver anropas explicit.
/// </summary>
public sealed class ValueGeneratedNeverTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>
    /// Kärntestet: det fel som rapporterades i produktion.
    /// Tilldelar en person till ett pass utan explicit MarkAsAdded och verifierar
    /// att tilldelningen faktiskt sparas i databasen.
    /// </summary>
    [Fact]
    public async Task AssignPersonToShift_PersistsWithoutMarkAsAdded()
    {
        var (shiftId, personId) = await SetupShiftAndPersonAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        var response = await client.PostAsJsonAsync(
            $"/shifts/{shiftId}/assignments",
            new { personId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var shiftId_vo = new ShiftId(shiftId);
        var shift = await db.Shifts.Include(s => s.Assignments)
            .FirstAsync(s => s.Id == shiftId_vo);
        Assert.Single(shift.Assignments);
    }

    /// <summary>
    /// Skapar en station via API och verifierar att den sparas.
    /// Testar CreateStation-flödet som tidigare krävde MarkAsAdded(station).
    /// </summary>
    [Fact]
    public async Task CreateStation_PersistsWithoutMarkAsAdded()
    {
        var (editionId, staffAreaId) = await SetupEditionAndAreaAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        var response = await client.PostAsJsonAsync(
            $"/editions/{editionId}/stations",
            new { name = "Teststation", staffAreaId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var editionId_vo = new EditionId(editionId);
        var edition = await db.Editions.Include(e => e.Stations)
            .FirstAsync(e => e.Id == editionId_vo);
        Assert.Contains(edition.Stations, s => s.Name == "Teststation");
    }

    /// <summary>
    /// Skapar ett funktionsområde via API och verifierar att det sparas.
    /// Testar CreateStaffArea-flödet som tidigare krävde MarkAsAdded(staffArea).
    /// </summary>
    [Fact]
    public async Task CreateStaffArea_PersistsWithoutMarkAsAdded()
    {
        var (editionId, responsibleId) = await SetupEditionAsync();
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        var response = await client.PostAsJsonAsync(
            $"/editions/{editionId}/staff-areas",
            new { name = "Testområde", responsibleId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var editionId_vo = new EditionId(editionId);
        var edition = await db.Editions.Include(e => e.StaffAreas)
            .FirstAsync(e => e.Id == editionId_vo);
        Assert.Contains(edition.StaffAreas, a => a.Name == "Testområde");
    }

    // ── Hjälpmetoder ────────────────────────────────────────────────────────

    private async Task<(Guid shiftId, Guid personId)> SetupShiftAndPersonAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

        var conventionId = new ConventionId(Factory.SeededConventionId);
        var convention = await db.Conventions.Include(c => c.Administrators).FirstAsync(c => c.Id == conventionId);
        var admin = await db.Persons.FirstAsync(p => p.Email == AdminEmail);

        var period = new DatePeriod(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)));
        var edition = convention.CreateEdition("Test Edition", period, admin.Id, admin.Id);
        db.Editions.Add(edition);

        var area = edition.CreateStaffArea("Area", admin.Id);
        var station = edition.CreateStation("Station", area.Id);
        await db.SaveChangesAsync();

        // Skapa passet direkt via domänobjektet – undviker handler som kräver HTTP-kontext
        var timeSlot = new TimeSlot(DateTime.Today.AddHours(10), DateTime.Today.AddHours(18));
        var staffingRequirement = new StaffingRequirement(1, 4);
        var shift = new Shift(ShiftId.New(), station.Id, admin.Id, timeSlot, staffingRequirement);
        db.Shifts.Add(shift);

        // Skapa en andra person att tilldela (admin är redan ansvarig)
        var person = convention.CreatePerson("Testperson", $"test{Guid.NewGuid():N}@example.com");
        db.Persons.Add(person);
        await db.SaveChangesAsync();

        return (shift.Id.Value, person.Id.Value);
    }

    private async Task<(Guid editionId, Guid staffAreaId)> SetupEditionAndAreaAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

        var conventionId = new ConventionId(Factory.SeededConventionId);
        var convention = await db.Conventions.FirstAsync(c => c.Id == conventionId);
        var admin = await db.Persons.FirstAsync(p => p.Email == AdminEmail);

        var period = new DatePeriod(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)));
        var edition = convention.CreateEdition($"Edition {Guid.NewGuid():N}", period, admin.Id, admin.Id);
        db.Editions.Add(edition);

        var area = edition.CreateStaffArea("Befintligt område", admin.Id);
        await db.SaveChangesAsync();

        return (edition.Id.Value, area.Id.Value);
    }

    private async Task<(Guid editionId, Guid responsibleId)> SetupEditionAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

        var conventionId = new ConventionId(Factory.SeededConventionId);
        var convention = await db.Conventions.FirstAsync(c => c.Id == conventionId);
        var admin = await db.Persons.FirstAsync(p => p.Email == AdminEmail);

        var period = new DatePeriod(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)));
        var edition = convention.CreateEdition($"Edition {Guid.NewGuid():N}", period, admin.Id, admin.Id);
        db.Editions.Add(edition);
        await db.SaveChangesAsync();

        return (edition.Id.Value, admin.Id.Value);
    }
}
