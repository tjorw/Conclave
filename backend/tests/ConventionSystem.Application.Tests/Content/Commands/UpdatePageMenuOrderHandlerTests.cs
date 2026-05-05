using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Content.Commands.UpdatePageMenuOrder;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Content.Commands;

public sealed class UpdatePageMenuOrderHandlerTests
{
    private readonly IPageRepository _pageRepo = Substitute.For<IPageRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdatePageMenuOrderHandler _handler;

    public UpdatePageMenuOrderHandlerTests()
    {
        _handler = new UpdatePageMenuOrderHandler(_pageRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesMenuOrderAndSaves()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var page = new Page(PageId.New(), convention.Id, null, "info", "Info", "Text", showInPublicMenu: true);

        _currentUser.PersonId.Returns(admin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);
        _pageRepo.GetByIdAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);

        await _handler.Handle(new UpdatePageMenuOrderCommand(page.Id.Value, 5), default);

        Assert.Equal(5, page.MenuSortOrder);
        await _pageRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        var nonAdmin = convention.CreatePerson("User", "user@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        _currentUser.PersonId.Returns(nonAdmin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new UpdatePageMenuOrderCommand(Guid.NewGuid(), 1), default));
    }
}
