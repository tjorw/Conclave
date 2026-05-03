using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Content.Commands.PublishPage;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Content.Commands;

public sealed class PublishPageHandlerTests
{
    private readonly IPageRepository _pageRepo = Substitute.For<IPageRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly PublishPageHandler _handler;

    public PublishPageHandlerTests()
    {
        _handler = new PublishPageHandler(_pageRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesPageAndSaves()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var page = new Page(PageId.New(), convention.Id, null, "info", "Info", "Text");

        _currentUser.PersonId.Returns(admin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);
        _pageRepo.GetByIdAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);

        await _handler.Handle(new PublishPageCommand(page.Id.Value), default);

        Assert.True(page.IsPublished);
        await _pageRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
