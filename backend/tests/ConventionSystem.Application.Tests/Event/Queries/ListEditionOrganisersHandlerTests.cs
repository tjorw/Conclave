using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Application.Event.Queries.ListEditionOrganisers;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Queries;

public class ListEditionOrganisersHandlerTests
{
    private readonly IEventRepository _repo = Substitute.For<IEventRepository>();
    private readonly ListEditionOrganisersHandler _handler;

    public ListEditionOrganisersHandlerTests()
    {
        _handler = new ListEditionOrganisersHandler(_repo);
    }

    [Fact]
    public async Task Handle_DelegatesTo_ListOrganisersByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var expected = new List<EditionOrganiserDto>
        {
            new(Guid.NewGuid(), "Diana Ek", "diana@example.com", null, eventId, "D&D-kampanj", "Huvudarrangör"),
        };
        _repo.ListOrganisersByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionOrganisersQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListOrganisersByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoPublishedEvents()
    {
        var editionId = Guid.NewGuid();
        _repo.ListOrganisersByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(new List<EditionOrganiserDto>());

        var result = await _handler.Handle(new ListEditionOrganisersQuery(editionId), default);

        Assert.Empty(result);
    }
}
