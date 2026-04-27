using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.RemoveEdition;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class RemoveEditionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveEditionHandler _handler;

    public RemoveEditionHandlerTests()
    {
        _handler = new RemoveEditionHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesEditionGraph()
    {
        var (convention, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RemoveEditionCommand(edition.Id.Value), default);

        await _editionRepo.Received(1).DeleteGraphAndSaveAsync(edition.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveEdition_ThrowsInvalidOperationException()
    {
        var (convention, admin, edition) = Setup();
        convention.SetActiveEdition(edition.Id);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new RemoveEditionCommand(edition.Id.Value), default));

        await _editionRepo.DidNotReceiveWithAnyArgs().DeleteGraphAndSaveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_PerformerNotAdmin_ThrowsInvalidForbiddenException()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("Non Admin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new RemoveEditionCommand(edition.Id.Value), default));
    }

    private (Domain.Convention.Aggregates.Convention Convention,
        Domain.Convention.Entities.Person Admin,
        Domain.Convention.Aggregates.Edition Edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoordinator = convention.CreatePerson("Event", "event@example.com");
        var edition = convention.CreateEdition(
            "Konvent 2028",
            new DatePeriod(new DateOnly(2028, 3, 1), new DateOnly(2028, 3, 3)),
            staff.Id,
            eventCoordinator.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }
}
