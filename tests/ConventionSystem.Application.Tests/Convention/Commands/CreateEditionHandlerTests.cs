using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CreateEditionHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly CreateEditionHandler _handler;

    public CreateEditionHandlerTests()
    {
        _handler = new CreateEditionHandler(_conventionRepo, _personRepo, _editionRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person staffCoord,
             Domain.Convention.Entities.Person eventCoord) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(staffCoord.Id, Arg.Any<CancellationToken>()).Returns(staffCoord);
        _personRepo.GetByIdAsync(eventCoord.Id, Arg.Any<CancellationToken>()).Returns(eventCoord);

        return (convention, admin, staffCoord, eventCoord);
    }

    private CreateEditionCommand ValidCommand(Domain.Convention.Aggregates.Convention convention,
        Domain.Convention.Entities.Person admin,
        Domain.Convention.Entities.Person staffCoord,
        Domain.Convention.Entities.Person eventCoord) =>
        new(convention.Id.Value, "Konvent 2027",
            new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
            staffCoord.Id.Value, eventCoord.Id.Value, admin.Id.Value);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        var (convention, admin, staffCoord, eventCoord) = Setup();

        var id = await _handler.Handle(ValidCommand(convention, admin, staffCoord, eventCoord), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsEdition()
    {
        var (convention, admin, staffCoord, eventCoord) = Setup();

        await _handler.Handle(ValidCommand(convention, admin, staffCoord, eventCoord), default);

        await _editionRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Convention.Aggregates.Edition>(e =>
                e.Name == "Konvent 2027" &&
                e.Status == Domain.Convention.Enums.EditionStatus.Draft &&
                e.StaffCoordinatorId == staffCoord.Id &&
                e.EventCoordinatorId == eventCoord.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, staffCoord, eventCoord) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateEditionCommand(
                convention.Id.Value, "Konvent 2027",
                new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
                staffCoord.Id.Value, eventCoord.Id.Value, nonAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_InvalidDateRange_Throws()
    {
        var (convention, admin, staffCoord, eventCoord) = Setup();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new CreateEditionCommand(
                convention.Id.Value, "Konvent 2027",
                new DateOnly(2027, 3, 3), new DateOnly(2027, 3, 1),
                staffCoord.Id.Value, eventCoord.Id.Value, admin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_StaffCoordinatorFromOtherConvention_Throws()
    {
        var (convention, admin, _, eventCoord) = Setup();
        var other = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = other.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateEditionCommand(
                convention.Id.Value, "Konvent 2027",
                new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
                foreignPerson.Id.Value, eventCoord.Id.Value, admin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EventCoordinatorFromOtherConvention_Throws()
    {
        var (convention, admin, staffCoord, _) = Setup();
        var other = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = other.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateEditionCommand(
                convention.Id.Value, "Konvent 2027",
                new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3),
                staffCoord.Id.Value, foreignPerson.Id.Value, admin.Id.Value), default));
    }
}
