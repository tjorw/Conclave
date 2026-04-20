using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.ListEditionVisitors;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class ListEditionVisitorsHandlerTests
{
    private readonly IVisitorRegistrationRepository _repo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ListEditionVisitorsHandler _handler;

    public ListEditionVisitorsHandlerTests()
    {
        _handler = new ListEditionVisitorsHandler(_repo);
    }

    [Fact]
    public async Task Handle_DelegatesTo_ListConfirmedByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionVisitorDto>
        {
            new(Guid.NewGuid(), "Anna Larsson", "anna@example.com", null),
        };
        _repo.ListConfirmedByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionVisitorsQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListConfirmedByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoConfirmedVisitors()
    {
        var editionId = Guid.NewGuid();
        _repo.ListConfirmedByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(new List<EditionVisitorDto>());

        var result = await _handler.Handle(new ListEditionVisitorsQuery(editionId), default);

        Assert.Empty(result);
    }
}
