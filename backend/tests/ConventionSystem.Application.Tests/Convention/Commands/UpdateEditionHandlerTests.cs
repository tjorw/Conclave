using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.UpdateEdition;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class UpdateEditionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateEditionHandler _handler;

    public UpdateEditionHandlerTests()
    {
        _handler = new UpdateEditionHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesEditionDetails()
    {
        var (convention, admin, edition) = Setup();
        var newStaff = convention.CreatePerson("New Staff", "ns@example.com");
        var newEvt = convention.CreatePerson("New Event", "ne@example.com");
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateEditionCommand(
            edition.Id.Value, "Test 2028",
            new DateOnly(2028, 4, 1), new DateOnly(2028, 4, 3),
            newStaff.Id.Value, newEvt.Id.Value), default);

        Assert.Equal("Test 2028", edition.Name);
        Assert.Equal(new DateOnly(2028, 4, 1), edition.Period.StartDate);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateEditionCommand(
            edition.Id.Value, "Nytt namn",
            new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
            edition.StaffCoordinatorId!.Value.Value, edition.EventCoordinatorId!.Value.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateEditionCommand(
                Guid.NewGuid(), "Namn",
                new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
                Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "na@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateEditionCommand(
                edition.Id.Value, "Namn",
                new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
                Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
