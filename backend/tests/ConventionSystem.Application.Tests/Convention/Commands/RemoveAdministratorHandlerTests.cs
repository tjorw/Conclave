using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.RemoveAdministrator;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class RemoveAdministratorHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveAdministratorHandler _handler;

    public RemoveAdministratorHandlerTests()
    {
        _handler = new RemoveAdministratorHandler(_conventionRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person existingAdmin,
             Domain.Convention.Entities.Person secondAdmin)
        Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var existingAdmin = convention.RegisterPerson("Admin", "admin@example.com");
        var secondAdmin = convention.CreatePerson("Anna", "anna@example.com");
        convention.AddAdministrator(existingAdmin.Id, existingAdmin.Id);
        convention.AddAdministrator(secondAdmin.Id, existingAdmin.Id);

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(secondAdmin.Id, Arg.Any<CancellationToken>()).Returns(secondAdmin);

        return (convention, existingAdmin, secondAdmin);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesAdministrator()
    {
        var (convention, existingAdmin, secondAdmin) = Setup();
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, secondAdmin.Id.Value), default);

        Assert.False(convention.IsAdministrator(secondAdmin.Id));
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, existingAdmin, secondAdmin) = Setup();
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, secondAdmin.Id.Value), default);

        await _conventionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConventionNotFound_Throws()
    {
        _conventionRepo.GetByIdAsync(Arg.Any<ConventionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Convention?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RemoveAdministratorCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, secondAdmin) = Setup();
        var nonAdmin = convention.CreatePerson("Bob", "bob@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, secondAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonNotFound_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        _personRepo.GetByIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = otherConvention.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, foreignPerson.Id.Value), default));
    }

    [Fact]
    public async Task Handle_RemoveSelf_Throws()
    {
        var (convention, existingAdmin, _) = Setup();
        _personRepo.GetByIdAsync(existingAdmin.Id, Arg.Any<CancellationToken>()).Returns(existingAdmin);
        _currentUser.PersonId.Returns(existingAdmin.Id);

        await Assert.ThrowsAsync<CannotRemoveSelfAsAdministratorException>(
            () => _handler.Handle(new RemoveAdministratorCommand(convention.Id.Value, existingAdmin.Id.Value), default));
    }
}
