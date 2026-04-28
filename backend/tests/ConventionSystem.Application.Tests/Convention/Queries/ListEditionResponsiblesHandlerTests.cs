using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Queries;

public class ListEditionResponsiblesHandlerTests
{
    private readonly IEditionRepository _repo = Substitute.For<IEditionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ListEditionResponsiblesHandler _handler;

    public ListEditionResponsiblesHandlerTests()
    {
        _handler = new ListEditionResponsiblesHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_Admin_DelegatesTo_GetResponsiblesByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var expected = new List<EditionResponsibleDto>
        {
            new("Funktioneringskoordinator", personId, "Erik Lund", "erik@example.com"),
            new("Evenemangskoordinator", null, null, null),
        };
        _currentUser.IsAdmin.Returns(true);
        _repo.GetResponsiblesByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionResponsiblesQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).GetResponsiblesByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceptionStaff_ReturnsResponsibles()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionResponsibleDto>
        {
            new("Funktioneringskoordinator", null, null, null),
        };
        _currentUser.IsAdmin.Returns(false);
        _currentUser.IsReception.Returns(true);
        _repo.GetResponsiblesByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionResponsiblesQuery(editionId), default);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Handle_Unauthorized_ThrowsForbiddenException()
    {
        _currentUser.IsAdmin.Returns(false);
        _currentUser.IsReception.Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new ListEditionResponsiblesQuery(Guid.NewGuid()), default));

        await _repo.DidNotReceiveWithAnyArgs().GetResponsiblesByEditionIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Admin_ReturnsMinimumTwoRows_ForCoordinatorPositions()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionResponsibleDto>
        {
            new("Funktioneringskoordinator", null, null, null),
            new("Evenemangskoordinator", null, null, null),
        };
        _currentUser.IsAdmin.Returns(true);
        _repo.GetResponsiblesByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionResponsiblesQuery(editionId), default);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Position == "Funktioneringskoordinator");
        Assert.Contains(result, r => r.Position == "Evenemangskoordinator");
    }
}
