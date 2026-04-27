using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateCategory;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CreateCategoryHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateCategoryHandler _handler;

    public CreateCategoryHandlerTests()
    {
        _handler = new CreateCategoryHandler(_editionRepo, _conventionRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(responsible.Id, Arg.Any<CancellationToken>()).Returns(responsible);

        return (convention, admin, responsible, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCategoryToEdition()
    {
        var (_, admin, responsible, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CreateCategoryCommand(edition.Id.Value, "Brädspel", null, null, responsible.Id.Value), default);

        Assert.Single(edition.Categories);
        Assert.Equal("Brädspel", edition.Categories[0].Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCategoryId()
    {
        var (_, admin, responsible, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(new CreateCategoryCommand(edition.Id.Value, "Brädspel", null, null, responsible.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, responsible, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CreateCategoryCommand(edition.Id.Value, "Brädspel", null, null, responsible.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithCategoriesAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateCategoryCommand(Guid.NewGuid(), "Kategori", null, null, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, responsible, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CreateCategoryCommand(edition.Id.Value, "Kategori", null, null, responsible.Id.Value), default));
    }

    [Fact]
    public async Task Handle_ResponsibleFromOtherConvention_Throws()
    {
        var (_, admin, _, edition) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var outsider = otherConvention.CreatePerson("Outsider", "outsider@example.com");
        _personRepo.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateCategoryCommand(edition.Id.Value, "Kategori", null, null, outsider.Id.Value), default));
    }
}
