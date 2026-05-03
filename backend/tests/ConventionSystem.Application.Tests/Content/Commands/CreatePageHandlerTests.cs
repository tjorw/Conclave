using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Content.Commands.CreatePage;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Content.Commands;

public sealed class CreatePageHandlerTests
{
    private readonly IPageRepository _pageRepo = Substitute.For<IPageRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreatePageHandler _handler;

    public CreatePageHandlerTests()
    {
        _handler = new CreatePageHandler(_pageRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsPageAndSaves()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        _pageRepo.SlugExistsAsync(convention.Id, null, "info", null, Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(new CreatePageCommand("info", "Info", "**Text**", null), default);

        Assert.NotEqual(Guid.Empty, id);
        await _pageRepo.Received(1).AddAsync(
            Arg.Is<Domain.Content.Aggregates.Page>(p => p.Slug == "info" && p.Title == "Info"),
            Arg.Any<CancellationToken>());
        await _pageRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _) = SetupAdminConvention();
        var nonAdmin = convention.CreatePerson("User", "user@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CreatePageCommand("info", "Info", "Text", null), default));
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person admin) SetupAdminConvention()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);
        return (convention, admin);
    }
}
