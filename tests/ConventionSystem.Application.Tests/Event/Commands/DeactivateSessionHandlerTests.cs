using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.DeactivateSession;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class DeactivateSessionHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly DeactivateSessionHandler _handler;

    public DeactivateSessionHandlerTests()
    {
        _handler = new DeactivateSessionHandler(_eventRepo, _editionRepo, _conventionRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition, Domain.Event.Aggregates.Event ev,
             SessionId sessionId) Setup()
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
        var draft = ev.GetDraftVersion();
        draft.EditTitle("Rollspel");
        draft.EditDescription("Beskrivning");
        ev.SubmitForReview();
        ev.ApproveVersion(eventCoord.Id);

        var timeSlot = new TimeSlot(
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 14, 0, 0));
        var session = ev.CreateSession(venue.Id, timeSlot, 20, StartType.FixedTime);

        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, eventCoord, edition, ev, session.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_SessionBecomesInactive()
    {
        var (_, responsible, _, ev, sessionId) = Setup();

        await _handler.Handle(new DeactivateSessionCommand(ev.Id.Value, sessionId.Value, responsible.Id.Value), default);

        Assert.Equal(SessionStatus.Inactive, ev.Sessions.First(s => s.Id == sessionId).Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesSessionDeactivatedEvent()
    {
        var (_, responsible, _, ev, sessionId) = Setup();
        ev.ClearDomainEvents();

        await _handler.Handle(new DeactivateSessionCommand(ev.Id.Value, sessionId.Value, responsible.Id.Value), default);

        Assert.Single(ev.DomainEvents.OfType<SessionDeactivated>());
    }

    [Fact]
    public async Task Handle_AlreadyInactive_Throws()
    {
        var (_, responsible, _, ev, sessionId) = Setup();
        ev.DeactivateSession(sessionId, responsible.Id);
        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new DeactivateSessionCommand(ev.Id.Value, sessionId.Value, responsible.Id.Value), default));
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev, sessionId) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new DeactivateSessionCommand(ev.Id.Value, sessionId.Value, outsider.Id.Value), default));
    }
}
