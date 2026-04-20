using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.DomainEventHandlers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.DomainEventHandlers;

public class EventCancelledHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ISessionRegistrationRepository _regRepo = Substitute.For<ISessionRegistrationRepository>();
    private readonly EventCancelledHandler _handler;

    public EventCancelledHandlerTests()
    {
        _handler = new EventCancelledHandler(_eventRepo, _regRepo);
    }

    private static Domain.Event.Aggregates.Event CreatePublishedEventWithSession(out SessionId sessionId)
    {
        var responsible = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), responsible);
        ev.EditTitle("Rollspel");
        ev.EditDescription("Beskrivning");
        ev.SubmitForReview();
        ev.Approve(responsible);

        var session = ev.CreateSession(
            VenueId.New(),
            new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0)),
            20, Domain.Event.Enums.StartType.FixedTime);
        sessionId = session.Id;
        return ev;
    }

    private static SessionRegistration CreateConfirmedRegistration()
        => new(SessionRegistrationId.New(), SessionId.New(), PersonId.New(), TicketId.New());

    [Fact]
    public async Task Handle_EventWithSessions_CancelsRegistrations()
    {
        var ev = CreatePublishedEventWithSession(out var sessionId);
        var reg = CreateConfirmedRegistration();

        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _regRepo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration> { reg });

        await _handler.Handle(new EventCancelled(ev.Id, PersonId.New(), DateTimeOffset.UtcNow), default);

        Assert.Equal(SessionRegistrationStatus.Cancelled, reg.Status);
    }

    [Fact]
    public async Task Handle_EventWithSessions_CallsSave()
    {
        var ev = CreatePublishedEventWithSession(out var sessionId);
        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _regRepo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration> { CreateConfirmedRegistration() });

        await _handler.Handle(new EventCancelled(ev.Id, PersonId.New(), DateTimeOffset.UtcNow), default);

        await _regRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_DoesNotThrow()
    {
        _eventRepo.GetByIdWithSessionsAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await _handler.Handle(new EventCancelled(EventId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _regRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRegistrations_DoesNotCallSave()
    {
        var ev = CreatePublishedEventWithSession(out var sessionId);
        _eventRepo.GetByIdWithSessionsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _regRepo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration>());

        await _handler.Handle(new EventCancelled(ev.Id, PersonId.New(), DateTimeOffset.UtcNow), default);

        await _regRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
