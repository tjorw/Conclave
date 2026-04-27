using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class ChangeCategoryResponsibleHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ChangeCategoryResponsibleHandler _handler;

    public ChangeCategoryResponsibleHandlerTests()
    {
        _handler = new ChangeCategoryResponsibleHandler(_editionRepo, _conventionRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person newResponsible,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Category category) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var oldResponsible = convention.CreatePerson("Gammal ansvarig", "old@example.com");
        var newResponsible = convention.CreatePerson("Ny ansvarig", "new@example.com");

        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);
        var category = edition.CreateCategory("Brädspel", oldResponsible.Id);

        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(newResponsible.Id, Arg.Any<CancellationToken>()).Returns(newResponsible);

        return (convention, admin, newResponsible, edition, category);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesCategoryResponsible()
    {
        var (_, admin, newResponsible, edition, category) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new ChangeCategoryResponsibleCommand(
            edition.Id.Value, category.Id.Value, newResponsible.Id.Value), default);

        Assert.Equal(newResponsible.Id, category.ResponsibleId);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, newResponsible, edition, category) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new ChangeCategoryResponsibleCommand(
            edition.Id.Value, category.Id.Value, newResponsible.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithCategoriesAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ChangeCategoryResponsibleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, newResponsible, edition, category) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new ChangeCategoryResponsibleCommand(
                edition.Id.Value, category.Id.Value, newResponsible.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NewResponsibleFromOtherConvention_Throws()
    {
        var (_, admin, _, edition, category) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var outsider = otherConvention.CreatePerson("Outsider", "outsider@example.com");
        _personRepo.GetByIdAsync(outsider.Id, Arg.Any<CancellationToken>()).Returns(outsider);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ChangeCategoryResponsibleCommand(
                edition.Id.Value, category.Id.Value, outsider.Id.Value), default));
    }
}
