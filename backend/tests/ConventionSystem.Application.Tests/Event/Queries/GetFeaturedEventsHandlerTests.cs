using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Application.Event.Queries.GetFeaturedEvents;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Queries;

public class GetFeaturedEventsHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ISessionRegistrationRepository _sessionRegistrationRepo = Substitute.For<ISessionRegistrationRepository>();

    private readonly GetFeaturedEventsHandler _handler;

    public GetFeaturedEventsHandlerTests()
    {
        _handler = new GetFeaturedEventsHandler(_conventionRepo, _editionRepo, _eventRepo, _sessionRegistrationRepo);
    }

    [Fact]
    public async Task Handle_WithFeaturedEvents_ReturnsFeaturedOrderedBySortOrder()
    {
        var editionId = new EditionId(Guid.NewGuid());
        _conventionRepo.GetActiveEditionIdAsync(Arg.Any<CancellationToken>()).Returns(editionId);
        _editionRepo.GetProjectedByIdAsync(editionId, Arg.Any<CancellationToken>()).Returns(CreateEditionDto(editionId.Value));

        var olderSession = new SessionSummaryDto(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2027, 3, 2, 10, 0, 0), new DateTime(2027, 3, 2, 11, 0, 0), 30, "FixedTime", "Active");
        var newerSession = new SessionSummaryDto(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 11, 0, 0), 30, "FixedTime", "Active");

        _eventRepo.ListByEditionIdAsync(editionId, Arg.Any<CancellationToken>()).Returns([
            CreateEventSummary("Första", true, 2, olderSession),
            CreateEventSummary("Andra", true, 1, newerSession),
            CreateEventSummary("Tredje", false, null, olderSession)
        ]);

        _sessionRegistrationRepo
            .CountConfirmedBySessionIdsAsync(Arg.Any<IReadOnlyCollection<SessionId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<SessionId, int>());

        var result = await _handler.Handle(new GetFeaturedEventsQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("Andra", result[0].Title);
        Assert.Equal("Första", result[1].Title);
    }

    [Fact]
    public async Task Handle_WithoutFeaturedEvents_FallsBackToThreeLatestPublished()
    {
        var editionId = new EditionId(Guid.NewGuid());
        _conventionRepo.GetActiveEditionIdAsync(Arg.Any<CancellationToken>()).Returns(editionId);
        _editionRepo.GetProjectedByIdAsync(editionId, Arg.Any<CancellationToken>()).Returns(CreateEditionDto(editionId.Value));

        _eventRepo.ListByEditionIdAsync(editionId, Arg.Any<CancellationToken>()).Returns([
            CreateEventSummary("Ett", false, null),
            CreateEventSummary("Två", false, null),
            CreateEventSummary("Tre", false, null),
            CreateEventSummary("Fyra", false, null)
        ]);

        _sessionRegistrationRepo
            .CountConfirmedBySessionIdsAsync(Arg.Any<IReadOnlyCollection<SessionId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<SessionId, int>());

        var result = await _handler.Handle(new GetFeaturedEventsQuery(), default);

        Assert.Equal(3, result.Count);
    }

    private static EditionDto CreateEditionDto(Guid editionId)
        => new(
            editionId,
            Guid.NewGuid(),
            "Konvent 2027",
            new DateOnly(2027, 3, 1),
            new DateOnly(2027, 3, 3),
            "Published",
            true,
            true,
            true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            [new VenueDto(Guid.NewGuid(), "Sal 1", "A", null)],
            [],
            [],
            [],
            []);

    private static EventSummaryDto CreateEventSummary(
        string title,
        bool isFeatured,
        int? featuredSortOrder,
        SessionSummaryDto? session = null)
        => new(
            Guid.CreateVersion7(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Kategori",
            Guid.NewGuid(),
            "Arrangör",
            "Published",
            title,
            isFeatured,
            featuredSortOrder,
            session is null ? 0 : 1,
            0,
            "Beskrivning",
            [],
            session is null ? [] : [session]);
}
