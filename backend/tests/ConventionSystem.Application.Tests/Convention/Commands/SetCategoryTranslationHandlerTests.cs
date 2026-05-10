using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.SetCategoryTranslation;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class SetCategoryTranslationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetCategoryTranslationHandler _handler;

    public SetCategoryTranslationHandlerTests()
    {
        _handler = new SetCategoryTranslationHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Category category) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        var category = edition.CreateCategory("Brädspel", responsible.Id);

        _editionRepo.GetByIdWithCategoriesAndTranslationsAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, category);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsTranslation()
    {
        var (_, admin, edition, category) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new SetCategoryTranslationCommand(edition.Id.Value, category.Id.Value, "en", "Board Games"),
            default);

        Assert.Single(category.Translations);
        Assert.Equal("Board Games", category.Translations[0].Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition, category) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new SetCategoryTranslationCommand(edition.Id.Value, category.Id.Value, "en", "Board Games"),
            default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_ThrowsResourceNotFoundException()
    {
        _editionRepo.GetByIdWithCategoriesAndTranslationsAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new SetCategoryTranslationCommand(Guid.NewGuid(), Guid.NewGuid(), "en", "Board Games"),
                default));
    }

    [Fact]
    public async Task Handle_NonAdministrator_ThrowsForbiddenException()
    {
        var (convention, _, edition, category) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new SetCategoryTranslationCommand(edition.Id.Value, category.Id.Value, "en", "Board Games"),
                default));
    }

    [Fact]
    public async Task Handle_UnknownCategory_ThrowsCategoryNotFoundInEditionException()
    {
        var (_, admin, edition, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<CategoryNotFoundInEditionException>(
            () => _handler.Handle(
                new SetCategoryTranslationCommand(edition.Id.Value, Guid.NewGuid(), "en", "Board Games"),
                default));
    }
}
