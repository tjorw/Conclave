using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.UnwatchSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class UnwatchSessionHandlerTests
{
    private readonly ISessionWatchRepository _watchRepo = Substitute.For<ISessionWatchRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UnwatchSessionHandler _handler;

    public UnwatchSessionHandlerTests()
    {
        _handler = new UnwatchSessionHandler(_watchRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_RemovesWatchForCurrentUserAndSession()
    {
        var personId = PersonId.New();
        var sessionId = SessionId.New();
        _currentUser.PersonId.Returns(personId);

        await _handler.Handle(new UnwatchSessionCommand(sessionId.Value), default);

        await _watchRepo.Received(1).RemoveByPersonAndSessionAsync(
            personId,
            sessionId,
            Arg.Any<CancellationToken>());
    }
}
