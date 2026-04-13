using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ChangeCategory;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class ChangeCategoryHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ChangeCategoryHandler _handler;

    public ChangeCategoryHandlerTests()
    {
        _handler = new ChangeCategoryHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Category newCategory,
             Domain.Event.Aggregates.Event ev) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        edition.Publish(admin.Id);

        var originalCategory = edition.CreateCategory("Rollspel", eventCoord.Id);
        var newCategory = edition.CreateCategory("Brädspel", eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, originalCategory.Id, admin.Id);

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);

        return (convention, admin, edition, newCategory, ev);
    }

    [Fact]
    public async Task Handle_AdminChangesCategory_UpdatesCategoryId()
    {
        var (_, _, _, newCategory, ev) = Setup();

        await _handler.Handle(new ChangeCategoryCommand(ev.Id.Value, newCategory.Id.Value), default);

        Assert.Equal(newCategory.Id, ev.CategoryId);
    }

    [Fact]
    public async Task Handle_AdminChangesCategory_CallsSave()
    {
        var (_, _, _, newCategory, ev) = Setup();

        await _handler.Handle(new ChangeCategoryCommand(ev.Id.Value, newCategory.Id.Value), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsUnauthorized()
    {
        var (convention, _, _, newCategory, ev) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new ChangeCategoryCommand(ev.Id.Value, newCategory.Id.Value), default));
    }

    [Fact]
    public async Task Handle_CategoryNotInEdition_Throws()
    {
        var (_, _, _, _, ev) = Setup();
        var foreignCategoryId = CategoryId.New();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ChangeCategoryCommand(ev.Id.Value, foreignCategoryId.Value), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        var (_, _, _, newCategory, _) = Setup();
        var missingEventId = Guid.NewGuid();
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>()).Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ChangeCategoryCommand(missingEventId, newCategory.Id.Value), default));
    }
}
