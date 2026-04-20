using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.DeleteEvent;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class DeleteEventHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeleteEventHandler _handler;

    public DeleteEventHandlerTests()
    {
        _handler = new DeleteEventHandler(_eventRepo, _currentUser);
    }

    private Domain.Event.Aggregates.Event CreateEvent(EventStatus status = EventStatus.Draft)
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());

        if (status == EventStatus.Cancelled)
            ev.CancelEvent(PersonId.New());

        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(ev.LeadOrganiserId);
        _currentUser.IsAdmin.Returns(false);

        return ev;
    }

    [Fact]
    public async Task Handle_DraftEvent_CallsDelete()
    {
        var ev = CreateEvent(EventStatus.Draft);

        await _handler.Handle(new DeleteEventCommand(ev.Id.Value), default);

        await _eventRepo.Received(1).DeleteAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancelledEvent_Admin_CallsDelete()
    {
        var ev = CreateEvent(EventStatus.Cancelled);
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new DeleteEventCommand(ev.Id.Value), default);

        await _eventRepo.Received(1).DeleteAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancelledEvent_NonAdmin_Throws()
    {
        var ev = CreateEvent(EventStatus.Cancelled);

        await Assert.ThrowsAsync<EventCannotBeDeletedException>(
            () => _handler.Handle(new DeleteEventCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_UnderReviewEvent_Throws()
    {
        var ev = CreateEvent(EventStatus.Draft);
        ev.EditTitle("Test");
        ev.EditDescription("Beskrivning");
        ev.SubmitForReview();
        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<EventCannotBeDeletedException>(
            () => _handler.Handle(new DeleteEventCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NotOrganiser_NonAdmin_Throws()
    {
        var ev = CreateEvent(EventStatus.Draft);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new DeleteEventCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NotOrganiser_Admin_CallsDelete()
    {
        var ev = CreateEvent(EventStatus.Draft);
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new DeleteEventCommand(ev.Id.Value), default);

        await _eventRepo.Received(1).DeleteAsync(ev, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdWithCoOrganisersAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new DeleteEventCommand(Guid.NewGuid()), default));
    }
}
