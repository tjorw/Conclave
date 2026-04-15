using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Convention.Aggregates;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class PublishEditionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly PublishEditionHandler _handler;

    public PublishEditionHandlerTests()
    {
        _handler = new PublishEditionHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesEdition()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new PublishEditionCommand(edition.Id.Value), default);

        Assert.Equal(EditionStatus.Published, edition.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new PublishEditionCommand(edition.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new PublishEditionCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new PublishEditionCommand(edition.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyPublished_Throws()
    {
        var (_, admin, edition) = Setup();
        edition.Publish(admin.Id);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<EditionAlreadyPublishedException>(
            () => _handler.Handle(new PublishEditionCommand(edition.Id.Value), default));
    }
}
