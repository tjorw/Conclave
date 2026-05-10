using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.SetProgramTagTranslation;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class SetProgramTagTranslationHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetProgramTagTranslationHandler _handler;

    public SetProgramTagTranslationHandlerTests()
    {
        _handler = new SetProgramTagTranslationHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             string tagName) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        var tagName = "Familjevänligt";
        edition.AddProgramTagDefinition(tagName);

        _editionRepo.GetByIdWithProgramTagTranslationsAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, tagName);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsTranslation()
    {
        var (_, admin, edition, tagName) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new SetProgramTagTranslationCommand(edition.Id.Value, tagName, "en", "Family Friendly"),
            default);

        Assert.Single(edition.ProgramTagTranslations);
        Assert.Equal("Family Friendly", edition.ProgramTagTranslations[0].TranslatedName);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, edition, tagName) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new SetProgramTagTranslationCommand(edition.Id.Value, tagName, "en", "Family Friendly"),
            default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_ThrowsResourceNotFoundException()
    {
        _editionRepo.GetByIdWithProgramTagTranslationsAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new SetProgramTagTranslationCommand(Guid.NewGuid(), "SomeTag", "en", "Translation"),
                default));
    }

    [Fact]
    public async Task Handle_NonAdministrator_ThrowsForbiddenException()
    {
        var (convention, _, edition, tagName) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new SetProgramTagTranslationCommand(edition.Id.Value, tagName, "en", "Family Friendly"),
                default));
    }

    [Fact]
    public async Task Handle_UnknownTag_ThrowsProgramTagDefinitionNotFoundException()
    {
        var (_, admin, edition, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<ProgramTagDefinitionNotFoundException>(
            () => _handler.Handle(
                new SetProgramTagTranslationCommand(edition.Id.Value, "NonExistentTag", "en", "Translation"),
                default));
    }
}
