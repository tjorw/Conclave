using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetMyWatchedSessionsHandlerTests
{
    private readonly ISessionWatchRepository _watchRepo = Substitute.For<ISessionWatchRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyWatchedSessionsHandler _handler;

    public GetMyWatchedSessionsHandlerTests()
    {
        _handler = new GetMyWatchedSessionsHandler(_watchRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithCurrentUserAndEdition()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();
        var expected = new List<MyWatchedSessionSummaryDto>
        {
            new(Guid.NewGuid(), "Nattspel", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), "Sal A", DateTimeOffset.UtcNow),
        };

        _currentUser.PersonId.Returns(personId);
        _watchRepo.ListByPersonAndEditionAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetMyWatchedSessionsQuery(editionId), default);

        Assert.Equal(expected, result);
        await _watchRepo.Received(1).ListByPersonAndEditionAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>());
    }
}
