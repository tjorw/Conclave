using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Commands.UpdateShift;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.Commands;

public class UpdateShiftHandlerTests
{
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IStaffApplicationRepository _staffApplicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateShiftHandler _handler;

    public UpdateShiftHandlerTests()
    {
        _handler = new UpdateShiftHandler(
            _shiftRepo,
            _editionRepo,
            _conventionRepo,
            _personRepo,
            _staffApplicationRepo,
            _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Station station,
             Domain.Convention.Entities.Station otherStation,
             Shift shift) Setup()
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
        var otherStation = edition.CreateStation("Reception B", staffArea.Id);

        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0));
        var requirement = new StaffingRequirement(1, 3);
        var shift = new Shift(ShiftId.New(), station.Id, admin.Id, timeSlot, requirement);

        _shiftRepo.GetByIdAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);
        _editionRepo.GetByStationIdAsync(shift.StationId, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(responsible.Id, Arg.Any<CancellationToken>()).Returns(responsible);
        _staffApplicationRepo.HasApprovedApplicationAsync(responsible.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(true);

        return (convention, admin, responsible, edition, station, otherStation, shift);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesShift()
    {
        var (_, admin, responsible, _, _, otherStation, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateShiftCommand(
            shift.Id.Value,
            otherStation.Id.Value,
            responsible.Id.Value,
            new DateTime(2027, 3, 1, 12, 0, 0),
            new DateTime(2027, 3, 1, 16, 0, 0),
            2,
            5), default);

        Assert.Equal(otherStation.Id, shift.StationId);
        Assert.Equal(responsible.Id, shift.ResponsibleId);
        Assert.Equal(new DateTime(2027, 3, 1, 12, 0, 0), shift.TimeSlot.Start);
        Assert.Equal(new DateTime(2027, 3, 1, 16, 0, 0), shift.TimeSlot.End);
        Assert.Equal(2, shift.StaffingRequirement.MinPersons);
        Assert.Equal(5, shift.StaffingRequirement.MaxPersons);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, responsible, _, _, otherStation, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateShiftCommand(
            shift.Id.Value,
            otherStation.Id.Value,
            responsible.Id.Value,
            new DateTime(2027, 3, 1, 12, 0, 0),
            new DateTime(2027, 3, 1, 16, 0, 0),
            2,
            5), default);

        await _shiftRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftNotFound_Throws()
    {
        _shiftRepo.GetByIdAsync(Arg.Any<ShiftId>(), Arg.Any<CancellationToken>())
            .Returns((Shift?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new UpdateShiftCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                2,
                5), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAuthorized_Throws()
    {
        var (convention, _, responsible, _, _, otherStation, shift) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new UpdateShiftCommand(
                shift.Id.Value,
                otherStation.Id.Value,
                responsible.Id.Value,
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                2,
                5), default));
    }

    [Fact]
    public async Task Handle_TargetStationOutsideEdition_Throws()
    {
        var (_, admin, responsible, _, _, _, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UpdateShiftCommand(
                shift.Id.Value,
                Guid.NewGuid(),
                responsible.Id.Value,
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                2,
                5), default));
    }

    [Fact]
    public async Task Handle_ResponsibleNotEditionStaff_Throws()
    {
        var (_, admin, responsible, edition, _, otherStation, shift) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _staffApplicationRepo.HasApprovedApplicationAsync(responsible.Id, edition.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UpdateShiftCommand(
                shift.Id.Value,
                otherStation.Id.Value,
                responsible.Id.Value,
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                2,
                5), default));
    }
}
