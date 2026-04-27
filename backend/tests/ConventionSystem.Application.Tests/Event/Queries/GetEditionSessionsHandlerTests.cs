using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Application.Event.Queries.GetEditionSessions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Queries;

public class GetEditionSessionsHandlerTests
{
    private readonly IEventRepository _repo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetEditionSessionsHandler _handler;

    public GetEditionSessionsHandlerTests()
    {
        _handler = new GetEditionSessionsHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_Admin_DelegatesTo_ListSessionsByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionSessionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "D&D-kampanj", Guid.NewGuid(),
                DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 20, "FixedTime", "Active"),
        };
        _currentUser.IsAdmin.Returns(true);
        _repo.ListSessionsByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetEditionSessionsQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListSessionsByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceptionStaff_ReturnsSessions()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionSessionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Rollspel", Guid.NewGuid(),
                DateTime.UtcNow, DateTime.UtcNow.AddHours(3), 10, "FixedTime", "Active"),
        };
        _currentUser.IsAdmin.Returns(false);
        _currentUser.IsReception.Returns(true);
        _repo.ListSessionsByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetEditionSessionsQuery(editionId), default);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Handle_Unauthorized_ThrowsForbiddenException()
    {
        _currentUser.IsAdmin.Returns(false);
        _currentUser.IsReception.Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new GetEditionSessionsQuery(Guid.NewGuid()), default));

        await _repo.DidNotReceiveWithAnyArgs().ListSessionsByEditionIdAsync(default!, default);
    }
}
