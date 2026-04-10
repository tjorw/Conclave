using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Commands.AssignPersonToShift;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.Commands;

public class AssignPersonToShiftHandlerTests
{
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AssignPersonToShiftHandler _handler;

    public AssignPersonToShiftHandlerTests()
    {
        _handler = new AssignPersonToShiftHandler(_shiftRepo, _editionRepo, _conventionRepo, _personRepo,
            _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person person,
             Domain.Convention.Aggregates.Edition edition,
             Shift shift) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var person = convention.CreatePerson("Person", "person@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        var staffArea = edition.CreateStaffArea("Reception", areaResponsible.Id);
        var station = edition.CreateStation("Reception A", staffArea.Id);

        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0));
        var requirement = new StaffingRequirement(1, 3);
        var shift = new Shift(ShiftId.New(), station.Id, admin.Id, timeSlot, requirement);

        _shiftRepo.GetByIdWithAssignmentsAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        _editionRepo.GetByStationIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        return (convention, admin, person, edition, shift);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAssignmentId()
    {
        var (_, admin, person, _, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, person.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, person, _, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, person.Id.Value), default);

        await _shiftRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftNotFound_Throws()
    {
        _shiftRepo.GetByIdWithAssignmentsAsync(Arg.Any<ShiftId>(), Arg.Any<CancellationToken>())
            .Returns((Shift?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AssignPersonToShiftCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAuthorized_Throws()
    {
        var (convention, _, person, _, shift) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, person.Id.Value), default));
    }

    [Fact]
    public async Task Handle_StaffCoordinatorCanAssign()
    {
        var (_, _, person, edition, shift) = Setup();
        var staffCoordId = edition.StaffCoordinatorId!.Value;
        _currentUser.PersonId.Returns(staffCoordId);

        var id = await _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, person.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_StaffAreaResponsibleCanAssign()
    {
        var (_, _, person, edition, shift) = Setup();
        var areaResponsibleId = edition.StaffAreas[0].ResponsibleId;
        _currentUser.PersonId.Returns(areaResponsibleId);

        var id = await _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, person.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (_, admin, _, _, shift) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var outsider = otherConvention.CreatePerson("Outsider", "outsider@example.com");
        _personRepo.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AssignPersonToShiftCommand(shift.Id.Value, outsider.Id.Value), default));
    }
}
