using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.AddReceptionStaff;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class AddReceptionStaffHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddReceptionStaffHandler _handler;

    public AddReceptionStaffHandlerTests()
    {
        _handler = new AddReceptionStaffHandler(_editionRepo, _conventionRepo, _personRepo, _currentUser);
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

        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(receptionist.Id, Arg.Any<CancellationToken>()).Returns(receptionist);

        return (convention, edition, admin, receptionist);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsReceptionStaff()
    {
        var (_, edition, admin, receptionist) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, receptionist.Id.Value), default);

        Assert.True(edition.IsReceptionStaff(receptionist.Id));
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, edition, admin, receptionist) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, receptionist.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithReceptionStaffAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddReceptionStaffCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdmin_Throws()
    {
        var (convention, edition, _, receptionist) = Setup();
        var nonAdmin = convention.CreatePerson("Bob", "bob@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, receptionist.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonNotFound_Throws()
    {
        var (_, edition, admin, _) = Setup();
        _personRepo.GetByIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (_, edition, admin, _) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = otherConvention.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, foreignPerson.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyReceptionStaff_Throws()
    {
        var (_, edition, admin, receptionist) = Setup();
        edition.AddReceptionStaff(receptionist.Id, admin.Id);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<PersonAlreadyReceptionStaffException>(
            () => _handler.Handle(new AddReceptionStaffCommand(edition.Id.Value, receptionist.Id.Value), default));
    }
}
