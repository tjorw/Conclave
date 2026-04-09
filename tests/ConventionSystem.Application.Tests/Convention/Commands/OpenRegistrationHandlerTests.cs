using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.OpenRegistration;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class OpenRegistrationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly OpenRegistrationHandler _handler;

    public OpenRegistrationHandlerTests()
    {
        _handler = new OpenRegistrationHandler(_editionRepo, _conventionRepo);
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
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);
        edition.Publish(admin.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Theory]
    [InlineData(RegistrationType.Organiser)]
    [InlineData(RegistrationType.Staff)]
    [InlineData(RegistrationType.Visitor)]
    public async Task Handle_ValidCommand_SetsCorrectFlag(RegistrationType type)
    {
        var (_, admin, edition) = Setup();

        await _handler.Handle(new OpenRegistrationCommand(edition.Id.Value, type, admin.Id.Value), default);

        if (type == RegistrationType.Organiser) Assert.True(edition.OrganiserRegistrationOpen);
        else if (type == RegistrationType.Staff) Assert.True(edition.StaffRegistrationOpen);
        else Assert.True(edition.VisitorRegistrationOpen);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition) = Setup();

        await _handler.Handle(new OpenRegistrationCommand(edition.Id.Value, RegistrationType.Staff, admin.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new OpenRegistrationCommand(Guid.NewGuid(), RegistrationType.Staff, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new OpenRegistrationCommand(edition.Id.Value, RegistrationType.Staff, nonAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyOpen_Throws()
    {
        var (_, admin, edition) = Setup();
        edition.OpenVisitorRegistration(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new OpenRegistrationCommand(edition.Id.Value, RegistrationType.Visitor, admin.Id.Value), default));
    }
}
