using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMyTeamAssignedSessions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public sealed class GetMyTeamAssignedSessionsHandlerTests
{
    private readonly IMyScheduleRepository _repository = Substitute.For<IMyScheduleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyTeamAssignedSessionsHandler _handler;

    public GetMyTeamAssignedSessionsHandlerTests()
    {
        _handler = new GetMyTeamAssignedSessionsHandler(_repository, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var personId = PersonId.New();
        var editionId = EditionId.New();
        var expected = new List<MyTeamAssignedSessionDto>
        {
            new(Guid.NewGuid(), "Lag Alpha", "Strategispel 2027",
                new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 12, 0, 0), "Sal 3")
        };

        _currentUser.PersonId.Returns(personId);
        _repository
            .ListMyTeamAssignedSessionsAsync(personId, editionId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MyTeamAssignedSessionDto>)expected);

        var result = await _handler.Handle(new GetMyTeamAssignedSessionsQuery(editionId.Value), default);

        Assert.Single(result);
        Assert.Equal("Lag Alpha", result[0].TeamName);
    }

    [Fact]
    public async Task Handle_NoAssignments_ReturnsEmpty()
    {
        var personId = PersonId.New();
        var editionId = EditionId.New();

        _currentUser.PersonId.Returns(personId);
        _repository
            .ListMyTeamAssignedSessionsAsync(personId, editionId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MyTeamAssignedSessionDto>)[]);

        var result = await _handler.Handle(new GetMyTeamAssignedSessionsQuery(editionId.Value), default);

        Assert.Empty(result);
    }
}
