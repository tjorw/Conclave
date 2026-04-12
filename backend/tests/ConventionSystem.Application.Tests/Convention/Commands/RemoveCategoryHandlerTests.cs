using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.RemoveCategory;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class RemoveCategoryHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveCategoryHandler _handler;

    public RemoveCategoryHandlerTests()
    {
        _handler = new RemoveCategoryHandler(_editionRepo, _conventionRepo, _currentUser);
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
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesCategoryFromEdition()
    {
        var (convention, admin, edition) = Setup();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RemoveCategoryCommand(edition.Id.Value, category.Id.Value), default);

        Assert.Empty(edition.Categories);
    }

    [Fact]
    public async Task Handle_ValidCommand_MarksCategoryAsRemoved()
    {
        var (convention, admin, edition) = Setup();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RemoveCategoryCommand(edition.Id.Value, category.Id.Value), default);

        _editionRepo.Received(1).MarkAsRemoved(category);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, admin, edition) = Setup();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RemoveCategoryCommand(edition.Id.Value, category.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithCategoriesAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new RemoveCategoryCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "na@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new RemoveCategoryCommand(edition.Id.Value, Guid.NewGuid()), default));
    }
}
