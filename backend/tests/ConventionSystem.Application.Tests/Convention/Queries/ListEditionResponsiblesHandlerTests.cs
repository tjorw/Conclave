using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Queries;

public class ListEditionResponsiblesHandlerTests
{
    private readonly IEditionRepository _repo = Substitute.For<IEditionRepository>();
    private readonly ListEditionResponsiblesHandler _handler;

    public ListEditionResponsiblesHandlerTests()
    {
        _handler = new ListEditionResponsiblesHandler(_repo);
    }

    [Fact]
    public async Task Handle_DelegatesTo_GetResponsiblesByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var expected = new List<EditionResponsibleDto>
        {
            new("Bemanningskoordinator", personId, "Erik Lund", "erik@example.com"),
            new("Evenemangskoordinator", null, null, null),
        };
        _repo.GetResponsiblesByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionResponsiblesQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetResponsiblesByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsMinimumTwoRows_ForCoordinatorPositions()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionResponsibleDto>
        {
            new("Bemanningskoordinator", null, null, null),
            new("Evenemangskoordinator", null, null, null),
        };
        _repo.GetResponsiblesByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionResponsiblesQuery(editionId), default);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Position == "Bemanningskoordinator");
        Assert.Contains(result, r => r.Position == "Evenemangskoordinator");
    }
}
