using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.AddAdministrator;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class AddAdministratorHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddAdministratorHandler _handler;

    public AddAdministratorHandlerTests()
    {
        _handler = new AddAdministratorHandler(_conventionRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person existingAdmin,
             Domain.Convention.Entities.Person newPerson) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var existingAdmin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(existingAdmin.Id, existingAdmin.Id);

        var newPerson = convention.CreatePerson("Anna", "anna@example.com");

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(newPerson.Id, Arg.Any<CancellationToken>()).Returns(newPerson);

        return (convention, existingAdmin, newPerson);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsAdministrator()
    {
        var (convention, existingAdmin, newPerson) = Setup();
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await _handler.Handle(new AddAdministratorCommand(convention.Id.Value, newPerson.Id.Value), default);

        Assert.True(convention.IsAdministrator(newPerson.Id));
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, existingAdmin, newPerson) = Setup();
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await _handler.Handle(new AddAdministratorCommand(convention.Id.Value, newPerson.Id.Value), default);

        await _conventionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConventionNotFound_Throws()
    {
        _conventionRepo.GetByIdAsync(Arg.Any<ConventionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Convention?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddAdministratorCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, newPerson) = Setup();
        var nonAdmin = convention.CreatePerson("Bob", "bob@example.com");
        _personRepo.GetByIdAsync(newPerson.Id, Arg.Any<CancellationToken>()).Returns(newPerson);
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddAdministratorCommand(convention.Id.Value, newPerson.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonNotFound_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        _personRepo.GetByIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddAdministratorCommand(convention.Id.Value, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = otherConvention.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddAdministratorCommand(convention.Id.Value, foreignPerson.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyAdministrator_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        _personRepo.GetByIdAsync(existingAdmin.Id, Arg.Any<CancellationToken>()).Returns(existingAdmin);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new AddAdministratorCommand(convention.Id.Value, existingAdmin.Id.Value), default));
    }
}
