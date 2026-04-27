using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Commands.CreateWalkupPerson;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CreateWalkupPersonHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly CreateWalkupPersonHandler _handler;

    public CreateWalkupPersonHandlerTests()
    {
        _handler = new CreateWalkupPersonHandler(_editionRepo, _conventionRepo, _currentUser, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person receptionist) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evtCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staffCoord.Id, evtCoord.Id);
        var receptionist = convention.CreatePerson("Receptionist", "reception@example.com");
        edition.AddReceptionStaff(receptionist.Id, admin.Id);

        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.EmailExistsInConventionAsync(convention.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        return (convention, edition, admin, receptionist);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsPersonId()
    {
        var (_, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var personId = await _handler.Handle(
            new CreateWalkupPersonCommand(edition.Id.Value, "Ny Person", "ny@example.com", null), default);

        Assert.NotEqual(Guid.Empty, personId);
        await _personRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Convention.Entities.Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceptionStaffMember_HasAccess()
    {
        var (_, edition, _, receptionist) = Setup();
        _currentUser.PersonId.Returns(receptionist.Id);

        var personId = await _handler.Handle(
            new CreateWalkupPersonCommand(edition.Id.Value, "Ny", "ny2@example.com", null), default);

        Assert.NotEqual(Guid.Empty, personId);
    }

    [Fact]
    public async Task Handle_NonReceptionNonAdmin_ThrowsForbiddenException()
    {
        var (convention, edition, _, _) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new CreateWalkupPersonCommand(edition.Id.Value, "Test", "test@example.com", null), default));
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var (convention, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _personRepo.EmailExistsInConventionAsync(convention.Id, "exists@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new CreateWalkupPersonCommand(edition.Id.Value, "Test", "exists@example.com", null), default));
    }
}
