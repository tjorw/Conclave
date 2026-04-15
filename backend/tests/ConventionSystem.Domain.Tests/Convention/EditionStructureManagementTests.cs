using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionStructureManagementTests
{
    private static (Domain.Convention.Aggregates.Convention convention,
                    Domain.Convention.Entities.Person admin,
                    Domain.Convention.Aggregates.Edition edition) CreateEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        return (convention, admin, edition);
    }

    // ── UpdateDetails ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_ValidInput_UpdatesAllProperties()
    {
        var (convention, _, edition) = CreateEdition();
        var newStaff = convention.CreatePerson("New Staff", "ns@example.com");
        var newEvt = convention.CreatePerson("New Event", "ne@example.com");
        var newPeriod = new DatePeriod(new DateOnly(2028, 4, 1), new DateOnly(2028, 4, 3));

        edition.UpdateDetails("Test 2028", newPeriod, newStaff.Id, newEvt.Id);

        Assert.Equal("Test 2028", edition.Name);
        Assert.Equal(new DateOnly(2028, 4, 1), edition.Period.StartDate);
        Assert.Equal(newStaff.Id, edition.StaffCoordinatorId);
        Assert.Equal(newEvt.Id, edition.EventCoordinatorId);
    }

    [Fact]
    public void UpdateDetails_EmptyName_Throws()
    {
        var (convention, _, edition) = CreateEdition();
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));

        Assert.Throws<ArgumentException>(() =>
            edition.UpdateDetails("", period, PersonId.New(), PersonId.New()));
    }

    // ── UpdateVenue ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateVenue_ValidInput_UpdatesProperties()
    {
        var (_, _, edition) = CreateEdition();
        var venue = edition.CreateVenue("Gamla salen", "Gamla huset", null);

        edition.UpdateVenue(venue.Id, "Nya salen", "Nya huset", "Renoverad");

        Assert.Equal("Nya salen", venue.Name);
        Assert.Equal("Nya huset", venue.Building);
        Assert.Equal("Renoverad", venue.Description);
    }

    [Fact]
    public void UpdateVenue_VenueNotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<VenueNotFoundInEditionException>(() =>
            edition.UpdateVenue(VenueId.New(), "Namn", "Byggnad", null));
    }

    // ── RemoveVenue ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveVenue_ExistingVenue_RemovesFromList()
    {
        var (_, _, edition) = CreateEdition();
        var venue = edition.CreateVenue("Salen", "Huset", null);

        edition.RemoveVenue(venue.Id);

        Assert.Empty(edition.Venues);
    }

    [Fact]
    public void RemoveVenue_ExistingVenue_ReturnsRemovedVenue()
    {
        var (_, _, edition) = CreateEdition();
        var venue = edition.CreateVenue("Salen", "Huset", null);

        var removed = edition.RemoveVenue(venue.Id);

        Assert.Equal(venue.Id, removed.Id);
    }

    [Fact]
    public void RemoveVenue_VenueNotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<VenueNotFoundInEditionException>(() => edition.RemoveVenue(VenueId.New()));
    }

    // ── UpdateStaffArea ─────────────────────────────────────────────────────

    [Fact]
    public void UpdateStaffArea_ValidInput_UpdatesProperties()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var area = edition.CreateStaffArea("Reception", responsible.Id, null);
        var newResponsible = convention.CreatePerson("Ny ansvarig", "b@example.com");

        edition.UpdateStaffArea(area.Id, "Entré", "Uppdaterad beskrivning", newResponsible.Id);

        Assert.Equal("Entré", area.Name);
        Assert.Equal("Uppdaterad beskrivning", area.Description);
        Assert.Equal(newResponsible.Id, area.ResponsibleId);
    }

    [Fact]
    public void UpdateStaffArea_NotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<StaffAreaNotFoundInEditionException>(() =>
            edition.UpdateStaffArea(StaffAreaId.New(), "Namn", null, PersonId.New()));
    }

    // ── RemoveStaffArea ─────────────────────────────────────────────────────

    [Fact]
    public void RemoveStaffArea_ExistingArea_RemovesFromList()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var area = edition.CreateStaffArea("Reception", responsible.Id, null);

        edition.RemoveStaffArea(area.Id);

        Assert.Empty(edition.StaffAreas);
    }

    [Fact]
    public void RemoveStaffArea_WithStations_CascadesRemovalOfStations()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var area = edition.CreateStaffArea("Reception", responsible.Id, null);
        edition.CreateStation("Ingång A", area.Id);
        edition.CreateStation("Ingång B", area.Id);

        var (_, removedStations) = edition.RemoveStaffArea(area.Id);

        Assert.Empty(edition.Stations);
        Assert.Equal(2, removedStations.Count);
    }

    [Fact]
    public void RemoveStaffArea_WithStations_DoesNotRemoveStationsFromOtherAreas()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var areaA = edition.CreateStaffArea("Area A", responsible.Id, null);
        var areaB = edition.CreateStaffArea("Area B", responsible.Id, null);
        edition.CreateStation("Station A", areaA.Id);
        edition.CreateStation("Station B", areaB.Id);

        edition.RemoveStaffArea(areaA.Id);

        Assert.Single(edition.Stations);
        Assert.Equal(areaB.Id, edition.Stations[0].StaffAreaId);
    }

    [Fact]
    public void RemoveStaffArea_NotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<StaffAreaNotFoundInEditionException>(() => edition.RemoveStaffArea(StaffAreaId.New()));
    }

    // ── UpdateCategory ──────────────────────────────────────────────────────

    [Fact]
    public void UpdateCategory_ValidInput_UpdatesProperties()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);
        var newResponsible = convention.CreatePerson("Ny ansvarig", "b@example.com");

        edition.UpdateCategory(category.Id, "Rollspel", "Uppdaterad", newResponsible.Id);

        Assert.Equal("Rollspel", category.Name);
        Assert.Equal("Uppdaterad", category.Description);
        Assert.Equal(newResponsible.Id, category.ResponsibleId);
    }

    [Fact]
    public void UpdateCategory_NotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<CategoryNotFoundInEditionException>(() =>
            edition.UpdateCategory(CategoryId.New(), "Namn", null, PersonId.New()));
    }

    // ── RemoveCategory ──────────────────────────────────────────────────────

    [Fact]
    public void RemoveCategory_ExistingCategory_RemovesFromList()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);

        edition.RemoveCategory(category.Id);

        Assert.Empty(edition.Categories);
    }

    [Fact]
    public void RemoveCategory_ExistingCategory_ReturnsRemovedCategory()
    {
        var (convention, _, edition) = CreateEdition();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);

        var removed = edition.RemoveCategory(category.Id);

        Assert.Equal(category.Id, removed.Id);
    }

    [Fact]
    public void RemoveCategory_NotFound_Throws()
    {
        var (_, _, edition) = CreateEdition();

        Assert.Throws<CategoryNotFoundInEditionException>(() => edition.RemoveCategory(CategoryId.New()));
    }
}
