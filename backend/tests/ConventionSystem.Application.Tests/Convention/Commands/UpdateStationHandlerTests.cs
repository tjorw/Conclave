using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.UpdateStation;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class UpdateStationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateStationHandler _handler;

    public UpdateStationHandlerTests()
    {
        _handler = new UpdateStationHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Station station,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Ansvarig", "area@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);
        var staffArea = edition.CreateStaffArea("Reception", areaResponsible.Id);
        var station = edition.CreateStation("Reception A", staffArea.Id);

        _editionRepo.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, station, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesStation()
    {
        var (_, admin, station, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateStationCommand(edition.Id.Value, station.Id.Value, "Reception B", "Ny beskrivning"), default);

        Assert.Equal("Reception B", edition.Stations[0].Name);
        Assert.Equal("Ny beskrivning", edition.Stations[0].Description);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, station, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateStationCommand(edition.Id.Value, station.Id.Value, "Reception B", null), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithStructureAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateStationCommand(Guid.NewGuid(), Guid.NewGuid(), "X", null), default));
    }

    [Fact]
    public async Task Handle_StationNotFound_Throws()
    {
        var (_, admin, _, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<StationNotFoundInEditionException>(
            () => _handler.Handle(new UpdateStationCommand(edition.Id.Value, Guid.NewGuid(), "X", null), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAuthorized_Throws()
    {
        var (convention, _, station, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateStationCommand(edition.Id.Value, station.Id.Value, "X", null), default));
    }

    [Fact]
    public async Task Handle_StaffCoordinatorCanUpdate()
    {
        var (_, _, station, edition) = Setup();
        _currentUser.PersonId.Returns(edition.StaffCoordinatorId!.Value);

        await _handler.Handle(new UpdateStationCommand(edition.Id.Value, station.Id.Value, "Reception B", null), default);

        Assert.Equal("Reception B", edition.Stations[0].Name);
    }
}
