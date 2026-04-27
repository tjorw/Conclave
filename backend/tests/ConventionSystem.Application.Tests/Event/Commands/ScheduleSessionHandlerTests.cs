using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ScheduleSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class ScheduleSessionHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ScheduleSessionHandler _handler;

    public ScheduleSessionHandlerTests()
    {
        _handler = new ScheduleSessionHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition, Domain.Event.Aggregates.Event ev,
             VenueId venueId) Setup()
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
        ev.Approve(admin.Id);

        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAndVenuesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, ev, venue.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSessionId()
    {
        var (_, admin, _, ev, venueId) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(new ScheduleSessionCommand(
            ev.Id.Value, venueId.Value,
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 14, 0, 0),
            20, StartType.FixedTime), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesSessionCreatedEvent()
    {
        var (_, admin, _, ev, venueId) = Setup();
        ev.ClearDomainEvents();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new ScheduleSessionCommand(
            ev.Id.Value, venueId.Value,
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 14, 0, 0),
            20, StartType.FixedTime), default);

        Assert.Single(ev.DomainEvents.OfType<SessionCreated>());
    }

    [Fact]
    public async Task Handle_VenueNotOnEdition_Throws()
    {
        var (_, admin, _, ev, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ScheduleSessionCommand(
                ev.Id.Value, Guid.NewGuid(),
                new DateTime(2027, 3, 1, 10, 0, 0),
                new DateTime(2027, 3, 1, 14, 0, 0),
                20, StartType.FixedTime), default));
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev, venueId) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new ScheduleSessionCommand(
                ev.Id.Value, venueId.Value,
                new DateTime(2027, 3, 1, 10, 0, 0),
                new DateTime(2027, 3, 1, 14, 0, 0),
                20, StartType.FixedTime), default));
    }
}
