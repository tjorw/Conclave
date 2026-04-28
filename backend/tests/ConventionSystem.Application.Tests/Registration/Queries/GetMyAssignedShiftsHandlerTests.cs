using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetMyAssignedShiftsHandlerTests
{
    private readonly IMyScheduleRepository _repo = Substitute.For<IMyScheduleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyAssignedShiftsHandler _handler;

    public GetMyAssignedShiftsHandlerTests()
    {
        _handler = new GetMyAssignedShiftsHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithCurrentUserAndEdition()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();
        var expected = new List<MyAssignedShiftSummaryDto>
        {
            new(Guid.NewGuid(), "Entré", "Responsible", DateTime.UtcNow, DateTime.UtcNow.AddHours(3)),
        };

        _currentUser.PersonId.Returns(personId);
        _repo.ListMyAssignedShiftsAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetMyAssignedShiftsQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListMyAssignedShiftsAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>());
    }
}
