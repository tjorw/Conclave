using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.WatchSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class WatchSessionHandlerTests
{
    private readonly ISessionWatchRepository _watchRepo = Substitute.For<ISessionWatchRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly WatchSessionHandler _handler;

    public WatchSessionHandlerTests()
    {
        _handler = new WatchSessionHandler(_watchRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidSession_AddsWatch()
    {
        var personId = PersonId.New();
        var sessionId = SessionId.New();
        var editionId = EditionId.New();

        _currentUser.PersonId.Returns(personId);
        _watchRepo.FindEditionIdBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(editionId);
        _watchRepo.ExistsAsync(personId, sessionId, Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.Handle(new WatchSessionCommand(sessionId.Value), default);

        await _watchRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Registration.Aggregates.SessionWatch>(w =>
                w.PersonId == personId && w.SessionId == sessionId && w.EditionId == editionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionNotFound_Throws()
    {
        var sessionId = SessionId.New();
        _currentUser.PersonId.Returns(PersonId.New());
        _watchRepo.FindEditionIdBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((EditionId?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new WatchSessionCommand(sessionId.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyWatched_DoesNotAddDuplicate()
    {
        var personId = PersonId.New();
        var sessionId = SessionId.New();

        _currentUser.PersonId.Returns(personId);
        _watchRepo.FindEditionIdBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(EditionId.New());
        _watchRepo.ExistsAsync(personId, sessionId, Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(new WatchSessionCommand(sessionId.Value), default);

        await _watchRepo.DidNotReceive().AddAndSaveAsync(Arg.Any<Domain.Registration.Aggregates.SessionWatch>(), Arg.Any<CancellationToken>());
    }
}
