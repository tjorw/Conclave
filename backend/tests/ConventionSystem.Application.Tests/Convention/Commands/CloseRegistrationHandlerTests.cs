using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CloseRegistration;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CloseRegistrationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CloseRegistrationHandler _handler;

    public CloseRegistrationHandlerTests()
    {
        _handler = new CloseRegistrationHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
                 Domain.Convention.Aggregates.Edition edition) Setup(
                     bool organiserOpen = true,
                     bool staffOpen = true,
                     bool visitorOpen = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);
        edition.Publish(admin.Id);

        if (organiserOpen) edition.OpenOrganiserRegistration(admin.Id);
        if (staffOpen) edition.OpenStaffRegistration(admin.Id);
        if (visitorOpen) edition.OpenVisitorRegistration(admin.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_CloseOrganiser_ClearsFlag()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Organiser), default);

        Assert.False(edition.OrganiserRegistrationOpen);
    }

    [Fact]
    public async Task Handle_CloseStaff_ClearsFlag()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Staff), default);

        Assert.False(edition.StaffRegistrationOpen);
    }

    [Fact]
    public async Task Handle_CloseVisitor_ClearsFlag()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Visitor), default);

        Assert.False(edition.VisitorRegistrationOpen);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Organiser), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CloseRegistrationCommand(Guid.NewGuid(), RegistrationType.Staff), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Staff), default));
    }

    [Fact]
    public async Task Handle_OrganiserNotOpen_Throws()
    {
        var (_, admin, edition) = Setup(organiserOpen: false);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<OrganiserRegistrationNotOpenException>(
            () => _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Organiser), default));
    }

    [Fact]
    public async Task Handle_StaffNotOpen_Throws()
    {
        var (_, admin, edition) = Setup(staffOpen: false);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<StaffRegistrationNotOpenException>(
            () => _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Staff), default));
    }

    [Fact]
    public async Task Handle_VisitorNotOpen_Throws()
    {
        var (_, admin, edition) = Setup(visitorOpen: false);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<VisitorRegistrationNotOpenException>(
            () => _handler.Handle(new CloseRegistrationCommand(edition.Id.Value, RegistrationType.Visitor), default));
    }
}
