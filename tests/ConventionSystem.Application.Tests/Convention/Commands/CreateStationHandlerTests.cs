using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateStation;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CreateStationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly CreateStationHandler _handler;

    public CreateStationHandlerTests()
    {
        _handler = new CreateStationHandler(_editionRepo, _conventionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(responsible.Id, Arg.Any<CancellationToken>()).Returns(responsible);

        return (convention, admin, responsible, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsStationToEdition()
    {
        var (_, admin, responsible, edition) = Setup();

        await _handler.Handle(new CreateStationCommand(edition.Id.Value, "Reception", null, responsible.Id.Value, admin.Id.Value), default);

        Assert.Single(edition.Stations);
        Assert.Equal("Reception", edition.Stations[0].Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsStationId()
    {
        var (_, admin, responsible, edition) = Setup();

        var id = await _handler.Handle(new CreateStationCommand(edition.Id.Value, "Reception", null, responsible.Id.Value, admin.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, responsible, edition) = Setup();

        await _handler.Handle(new CreateStationCommand(edition.Id.Value, "Reception", null, responsible.Id.Value, admin.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateStationCommand(Guid.NewGuid(), "Station", null, Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, responsible, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateStationCommand(edition.Id.Value, "Station", null, responsible.Id.Value, nonAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_ResponsibleFromOtherConvention_Throws()
    {
        var (_, admin, _, edition) = Setup();

        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var outsider = otherConvention.CreatePerson("Outsider", "outsider@example.com");
        _personRepo.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateStationCommand(edition.Id.Value, "Station", null, outsider.Id.Value, admin.Id.Value), default));
    }
}
