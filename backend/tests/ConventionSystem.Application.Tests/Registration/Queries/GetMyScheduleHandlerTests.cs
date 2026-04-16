using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMySchedule;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetMyScheduleHandlerTests
{
    private readonly IMyScheduleRepository _repo = Substitute.For<IMyScheduleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyScheduleHandler _handler;

    public GetMyScheduleHandlerTests()
    {
        _handler = new GetMyScheduleHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithCurrentUserAndEdition()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();
        var expected = new List<MyScheduleItemDto>
        {
            new(Guid.NewGuid(), null, "Nattspel", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), "Sal A", "Booked", true),
            new(null, Guid.NewGuid(), "Ingångsvakt", DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(5), "Ingång Nord", "Shift", true),
        };

        _currentUser.PersonId.Returns(personId);
        _repo.GetMyScheduleAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetMyScheduleQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetMyScheduleAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoEngagements()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();

        _currentUser.PersonId.Returns(personId);
        _repo.GetMyScheduleAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<MyScheduleItemDto>());

        var result = await _handler.Handle(new GetMyScheduleQuery(editionId), default);

        Assert.Empty(result);
    }
}
