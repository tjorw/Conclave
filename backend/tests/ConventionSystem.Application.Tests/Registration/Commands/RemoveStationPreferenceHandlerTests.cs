using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RemoveStationPreference;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RemoveStationPreferenceHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly RemoveStationPreferenceHandler _handler;

    public RemoveStationPreferenceHandlerTests()
    {
        _handler = new RemoveStationPreferenceHandler(_applicationRepo);
    }

    private (StaffApplication application, StationId stationId) SetupWithPreference()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        var staffArea = edition.CreateStaffArea("Reception", admin.Id);
        var station = edition.CreateStation("Info-disk", staffArea.Id);

        var applicant = convention.CreatePerson("Sökande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");
        application.AddStationPreference(station.Id);

        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);

        return (application, station.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesPreferenceAndSaves()
    {
        var (application, stationId) = SetupWithPreference();

        await _handler.Handle(new RemoveStationPreferenceCommand(application.Id.Value, stationId.Value), default);

        Assert.Empty(application.StationPreferences);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RemoveStationPreferenceCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
