using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Commands.CancelAssignment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.Commands;

public class CancelAssignmentHandlerTests
{
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelAssignmentHandler _handler;

    public CancelAssignmentHandlerTests()
    {
        _handler = new CancelAssignmentHandler(_shiftRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Shift shift) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
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

        return (convention, admin, edition, shift);
    }

    [Fact]
    public async Task Handle_AdminCancelsAssignment_CancelsAssignment()
    {
        var (_, admin, _, shift) = Setup();
        var assignment = shift.AssignPerson(PersonId.New(), admin.Id);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CancelAssignmentCommand(shift.Id.Value, assignment.Id.Value), default);

        Assert.Empty(shift.Assignments);
    }

    [Fact]
    public async Task Handle_AssignedPersonCancelsOwnAssignment_Succeeds()
    {
        var (_, admin, _, shift) = Setup();
        var personId = PersonId.New();
        var assignment = shift.AssignPerson(personId, admin.Id);
        _currentUser.PersonId.Returns(personId);

        await _handler.Handle(new CancelAssignmentCommand(shift.Id.Value, assignment.Id.Value), default);

        Assert.Empty(shift.Assignments);
    }

    [Fact]
    public async Task Handle_AssignedPersonCancelsOwnAssignment_DoesNotLoadEdition()
    {
        var (_, admin, _, shift) = Setup();
        var personId = PersonId.New();
        var assignment = shift.AssignPerson(personId, admin.Id);
        _currentUser.PersonId.Returns(personId);

        await _handler.Handle(new CancelAssignmentCommand(shift.Id.Value, assignment.Id.Value), default);

        await _editionRepo.DidNotReceive().GetByStationIdAsync(Arg.Any<StationId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnauthorizedPersonCancels_Throws()
    {
        var (convention, _, _, shift) = Setup();
        var assignment = shift.AssignPerson(PersonId.New(), PersonId.New());
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CancelAssignmentCommand(shift.Id.Value, assignment.Id.Value), default));
    }

    [Fact]
    public async Task Handle_ShiftNotFound_Throws()
    {
        _shiftRepo.GetByIdWithAssignmentsAsync(Arg.Any<ShiftId>(), Arg.Any<CancellationToken>())
            .Returns((Shift?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CancelAssignmentCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, _, shift) = Setup();
        var assignment = shift.AssignPerson(PersonId.New(), admin.Id);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CancelAssignmentCommand(shift.Id.Value, assignment.Id.Value), default);

        await _shiftRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
