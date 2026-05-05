using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.SetEditionContent;
using ConventionSystem.Domain.Convention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class SetEditionContentHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetEditionContentHandler _handler;

    public SetEditionContentHandlerTests()
    {
        _handler = new SetEditionContentHandler(_editionRepo, _conventionRepo, _currentUser);
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
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdWithContentAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidItems_SetsContentOnEdition()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new SetEditionContentCommand(
            edition.Id.Value,
            [new EditionContentItem(EditionContentKey.HeroTitle, "Välkommen!")]),
            default);

        Assert.Single(edition.Content);
        Assert.Equal("Välkommen!", edition.Content[0].Value);
    }

    [Fact]
    public async Task Handle_ValidItems_CallsSave()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new SetEditionContentCommand(
            edition.Id.Value,
            [new EditionContentItem(EditionContentKey.HeroTitle, "Välkommen!")]),
            default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleItems_SetsAllKeys()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new SetEditionContentCommand(
            edition.Id.Value,
            [
                new EditionContentItem(EditionContentKey.HeroTitle, "Titel"),
                new EditionContentItem(EditionContentKey.HeroIngress, "Ingress"),
                new EditionContentItem(EditionContentKey.CtaVisitorLabel, "Bli besökare"),
            ]),
            default);

        Assert.Equal(3, edition.Content.Count);
    }

    [Fact]
    public async Task Handle_UnknownKey_Throws()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SetEditionContentCommand(
                edition.Id.Value,
                [new EditionContentItem("okänd.nyckel", "värde")]),
                default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("Gäst", "guest@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new SetEditionContentCommand(
                edition.Id.Value,
                [new EditionContentItem(EditionContentKey.HeroTitle, "Titel")]),
                default));
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithContentAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SetEditionContentCommand(
                Guid.NewGuid(),
                [new EditionContentItem(EditionContentKey.HeroTitle, "Titel")]),
                default));
    }
}
