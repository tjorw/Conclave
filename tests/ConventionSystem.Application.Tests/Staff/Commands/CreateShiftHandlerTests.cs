using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Commands.CreateShift;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.Commands;

public class CreateShiftHandlerTests
{
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly CreateShiftHandler _handler;

    public CreateShiftHandlerTests()
    {
        _handler = new CreateShiftHandler(_shiftRepo, _editionRepo, _conventionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Station station) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var responsible = convention.CreatePerson("Skiftansvarig", "skift@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        var staffArea = edition.CreateStaffArea("Reception", areaResponsible.Id);
        var station = edition.CreateStation("Reception A", staffArea.Id);

        _editionRepo.GetByStationIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(responsible.Id, Arg.Any<CancellationToken>()).Returns(responsible);

        return (convention, admin, responsible, edition, station);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsShiftId()
    {
        var (_, admin, responsible, _, station) = Setup();

        var id = await _handler.Handle(new CreateShiftCommand(
            station.Id.Value, responsible.Id.Value,
            new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
            1, 3, admin.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var (_, admin, responsible, _, station) = Setup();

        await _handler.Handle(new CreateShiftCommand(
            station.Id.Value, responsible.Id.Value,
            new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
            1, 3, admin.Id.Value), default);

        await _shiftRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Staff.Aggregates.Shift>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StationNotFound_Throws()
    {
        _editionRepo.GetByStationIdAsync(Arg.Any<StationId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateShiftCommand(
                Guid.NewGuid(), Guid.NewGuid(),
                new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
                1, 3, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAuthorized_Throws()
    {
        var (convention, _, responsible, _, station) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateShiftCommand(
                station.Id.Value, responsible.Id.Value,
                new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
                1, 3, nonAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_StaffCoordinatorCanCreate()
    {
        var (_, _, responsible, edition, station) = Setup();
        var staffCoordId = edition.StaffCoordinatorId!.Value.Value;

        var id = await _handler.Handle(new CreateShiftCommand(
            station.Id.Value, responsible.Id.Value,
            new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
            1, 3, staffCoordId), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_StaffAreaResponsibleCanCreate()
    {
        var (_, _, responsible, edition, station) = Setup();
        var areaResponsibleId = edition.StaffAreas[0].ResponsibleId.Value;

        var id = await _handler.Handle(new CreateShiftCommand(
            station.Id.Value, responsible.Id.Value,
            new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
            1, 3, areaResponsibleId), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ResponsibleFromOtherConvention_Throws()
    {
        var (_, admin, _, _, station) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var outsider = otherConvention.CreatePerson("Outsider", "outsider@example.com");
        _personRepo.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateShiftCommand(
                station.Id.Value, outsider.Id.Value,
                new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0),
                1, 3, admin.Id.Value), default));
    }
}
