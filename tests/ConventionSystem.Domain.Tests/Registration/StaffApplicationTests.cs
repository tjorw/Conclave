using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class StaffApplicationTests
{
    private static StaffApplication CreateApplication()
        => new(StaffApplicationId.New(), PersonId.New(), EditionId.New(), "Intresserad av reception");

    [Fact]
    public void Constructor_SetsStatusReceived_RaisesEvent()
    {
        var application = CreateApplication();

        Assert.Equal(StaffApplicationStatus.Received, application.Status);
        Assert.Single(application.DomainEvents.OfType<StaffApplicationReceived>());
    }

    [Fact]
    public void Constructor_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new StaffApplication(StaffApplicationId.New(), PersonId.New(), EditionId.New(), ""));
    }

    [Fact]
    public void Accept_FromReceived_TransitionsToConfirmed()
    {
        var application = CreateApplication();
        application.ClearDomainEvents();

        application.Accept(PersonId.New());

        Assert.Equal(StaffApplicationStatus.Confirmed, application.Status);
        Assert.Single(application.DomainEvents.OfType<StaffApplicationAccepted>());
    }

    [Fact]
    public void Reject_FromReceived_TransitionsToRejected()
    {
        var application = CreateApplication();
        application.ClearDomainEvents();

        application.Reject(PersonId.New());

        Assert.Equal(StaffApplicationStatus.Rejected, application.Status);
        Assert.Single(application.DomainEvents.OfType<StaffApplicationRejected>());
    }

    [Fact]
    public void Accept_AlreadyConfirmed_Throws()
    {
        var application = CreateApplication();
        application.Accept(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => application.Accept(PersonId.New()));
    }

    [Fact]
    public void Reject_AlreadyRejected_Throws()
    {
        var application = CreateApplication();
        application.Reject(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => application.Reject(PersonId.New()));
    }

    [Fact]
    public void AddAvailability_AddsToList()
    {
        var application = CreateApplication();

        var availability = application.AddAvailability(
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 14, 0, 0));

        Assert.Single(application.Availabilities);
        Assert.Equal(availability.Id, application.Availabilities[0].Id);
    }

    [Fact]
    public void RemoveAvailability_RemovesFromList()
    {
        var application = CreateApplication();
        var availability = application.AddAvailability(
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 14, 0, 0));

        application.RemoveAvailability(availability.Id);

        Assert.Empty(application.Availabilities);
    }

    [Fact]
    public void AddStationPreference_AddsToList()
    {
        var application = CreateApplication();
        var stationId = StationId.New();

        application.AddStationPreference(stationId);

        Assert.Single(application.StationPreferences);
    }

    [Fact]
    public void AddStationPreference_Duplicate_Throws()
    {
        var application = CreateApplication();
        var stationId = StationId.New();
        application.AddStationPreference(stationId);

        Assert.Throws<InvalidOperationException>(() => application.AddStationPreference(stationId));
    }
}
