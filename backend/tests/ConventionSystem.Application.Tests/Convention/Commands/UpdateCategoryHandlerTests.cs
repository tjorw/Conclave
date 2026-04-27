using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.UpdateCategory;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class UpdateCategoryHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateCategoryHandler _handler;

    public UpdateCategoryHandlerTests()
    {
        _handler = new UpdateCategoryHandler(_editionRepo, _conventionRepo, _currentUser);
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
    public async Task Handle_ValidCommand_UpdatesCategory()
    {
        var (convention, admin, edition) = Setup();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null);
        var newResponsible = convention.CreatePerson("Ny ansvarig", "b@example.com");
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateCategoryCommand(
            edition.Id.Value, category.Id.Value, "Rollspel", "## Instruktioner", "Publik text", newResponsible.Id.Value), default);

        Assert.Equal("Rollspel", category.Name);
        Assert.Equal("## Instruktioner", category.OrganizerInstructions);
        Assert.Equal("Publik text", category.PublicDescription);
        Assert.Equal(newResponsible.Id, category.ResponsibleId);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, admin, edition) = Setup();
        var responsible = convention.CreatePerson("Ansvarig", "a@example.com");
        var category = edition.CreateCategory("Brädspel", responsible.Id, null, null);
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateCategoryCommand(
            edition.Id.Value, category.Id.Value, "Brädspel", null, null, responsible.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithCategoriesAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdateCategoryCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Namn", null, null, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "na@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new UpdateCategoryCommand(
                edition.Id.Value, Guid.NewGuid(), "Namn", null, null, Guid.NewGuid()), default));
    }
}
