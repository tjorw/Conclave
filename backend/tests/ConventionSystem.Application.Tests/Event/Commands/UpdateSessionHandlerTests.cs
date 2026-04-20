using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ScheduleSession;
using ConventionSystem.Application.Event.Commands.UpdateSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class UpdateSessionHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateSessionHandler _handler;

    public UpdateSessionHandlerTests()
    {
        _handler = new UpdateSessionHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition, Domain.Event.Aggregates.Event ev,
             VenueId venueId, SessionId sessionId) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", eventCoord.Id);
        var venue = edition.CreateVenue("Sal A", "Byggnad 1");

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        ev.EditTitle("Rollspel");
        ev.EditDescription("Beskrivning");
        ev.SubmitForReview();
        ev.Approve(eventCoord.Id);
        var session = ev.CreateSession(venue.Id,
            new Domain.Event.ValueObjects.TimeSlot(
                new DateTime(2027, 3, 1, 10, 0, 0),
                new DateTime(2027, 3, 1, 14, 0, 0)),
            20, StartType.FixedTime);

        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAndVenuesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, eventCoord, edition, ev, venue.Id, session.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesSession()
    {
        var (_, responsible, _, ev, venueId, sessionId) = Setup();
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new UpdateSessionCommand(
            ev.Id.Value, sessionId.Value, venueId.Value,
            new DateTime(2027, 3, 1, 12, 0, 0),
            new DateTime(2027, 3, 1, 16, 0, 0),
            30, StartType.Rolling), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
        var session = ev.Sessions.Single(s => s.Id == sessionId);
        Assert.Equal(30, session.MaxSeats);
        Assert.Equal(StartType.Rolling, session.StartType);
    }

    [Fact]
    public async Task Handle_SessionNotFound_Throws()
    {
        var (_, responsible, _, ev, venueId, _) = Setup();
        _currentUser.PersonId.Returns(responsible.Id);

        await Assert.ThrowsAsync<SessionNotFoundException>(() =>
            _handler.Handle(new UpdateSessionCommand(
                ev.Id.Value, Guid.NewGuid(), venueId.Value,
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                30, StartType.FixedTime), default));
    }

    [Fact]
    public async Task Handle_VenueNotOnEdition_Throws()
    {
        var (_, responsible, _, ev, _, sessionId) = Setup();
        _currentUser.PersonId.Returns(responsible.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UpdateSessionCommand(
                ev.Id.Value, sessionId.Value, Guid.NewGuid(),
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                30, StartType.FixedTime), default));
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev, venueId, sessionId) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new UpdateSessionCommand(
                ev.Id.Value, sessionId.Value, venueId.Value,
                new DateTime(2027, 3, 1, 12, 0, 0),
                new DateTime(2027, 3, 1, 16, 0, 0),
                30, StartType.FixedTime), default));
    }
}
