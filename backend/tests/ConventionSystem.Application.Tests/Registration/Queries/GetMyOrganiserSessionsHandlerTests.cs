using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetMyOrganiserSessionsHandlerTests
{
    private readonly IMyScheduleRepository _repo = Substitute.For<IMyScheduleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyOrganiserSessionsHandler _handler;

    public GetMyOrganiserSessionsHandlerTests()
    {
        _handler = new GetMyOrganiserSessionsHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithCurrentUserAndEdition()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();
        var expected = new List<MyOrganiserSessionSummaryDto>
        {
            new(Guid.NewGuid(), "Nattspel", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), "Sal A"),
        };

        _currentUser.PersonId.Returns(personId);
        _repo.ListMyOrganiserSessionsAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetMyOrganiserSessionsQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListMyOrganiserSessionsAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>());
    }
}
