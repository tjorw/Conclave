using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionCopyStructureTests
{
    private static (Domain.Convention.Aggregates.Convention convention,
                    Domain.Convention.Aggregates.Edition source,
                    Domain.Convention.Aggregates.Edition target) CreateSetup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");

        var period = new DatePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3));
        var source = convention.CreateEdition("Konvent 2026", period, staff.Id, evt.Id);
        source.CreateVenue("Stora salen", "Huvudbyggnad");
        source.CreateVenue("Lilla salen", "Annex");
        var staffArea = source.CreateStaffArea("Reception", areaResponsible.Id);
        source.CreateStation("Reception A", staffArea.Id);

        var period2 = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var target = convention.CreateEdition("Konvent 2027", period2, staff.Id, evt.Id);

        return (convention, source, target);
    }

    [Fact]
    public void CopyStructure_CopiesAllVenues()
    {
        var (_, source, target) = CreateSetup();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        Assert.Equal(2, target.Venues.Count);
        Assert.Contains(target.Venues, v => v.Name == "Stora salen");
        Assert.Contains(target.Venues, v => v.Name == "Lilla salen");
    }

    [Fact]
    public void CopyStructure_CopiesAllStaffAreas()
    {
        var (_, source, target) = CreateSetup();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        Assert.Single(target.StaffAreas);
        Assert.Equal("Reception", target.StaffAreas[0].Name);
    }

    [Fact]
    public void CopyStructure_CopiesAllStations()
    {
        var (_, source, target) = CreateSetup();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        Assert.Single(target.Stations);
        Assert.Equal("Reception A", target.Stations[0].Name);
    }

    [Fact]
    public void CopyStructure_RemapsStaffAreaIds()
    {
        var (_, source, target) = CreateSetup();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        var sourceAreaIds = source.StaffAreas.Select(sa => sa.Id).ToHashSet();
        Assert.DoesNotContain(target.Stations, s => sourceAreaIds.Contains(s.StaffAreaId));
    }

    [Fact]
    public void CopyStructure_AssignsNewIds()
    {
        var (_, source, target) = CreateSetup();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        var sourceVenueIds = source.Venues.Select(v => v.Id).ToHashSet();
        Assert.DoesNotContain(target.Venues, v => sourceVenueIds.Contains(v.Id));
    }

    [Fact]
    public void CopyStructure_OverwritesExistingStructure()
    {
        var (_, source, target) = CreateSetup();
        target.CreateVenue("Gammal lokal", "Gamla byggnaden");

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New());

        Assert.Equal(2, target.Venues.Count);
        Assert.DoesNotContain(target.Venues, v => v.Name == "Gammal lokal");
    }

    [Fact]
    public void CopyStructure_RaisesStructureCopiedEvent()
    {
        var (_, source, target) = CreateSetup();
        var performedById = PersonId.New();

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, performedById);

        var domainEvent = target.DomainEvents.OfType<StructureCopiedFromEdition>().Single();
        Assert.Equal(target.Id, domainEvent.TargetId);
        Assert.Equal(source.Id, domainEvent.SourceId);
        Assert.Equal(2, domainEvent.VenueCount);
        Assert.Equal(1, domainEvent.StaffAreaCount);
        Assert.Equal(1, domainEvent.StationCount);
        Assert.Equal(performedById, domainEvent.PerformedById);
    }

    [Fact]
    public void CopyStructure_PublishedEdition_Throws()
    {
        var (_, source, target) = CreateSetup();
        target.Publish(PersonId.New());

        Assert.Throws<EditionMustBeDraftToCopyStructureException>(
            () => target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, PersonId.New()));
    }
}
