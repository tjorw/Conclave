using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class SubmitForReviewHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly SubmitForReviewHandler _handler;

    public SubmitForReviewHandlerTests()
    {
        _handler = new SubmitForReviewHandler(_eventRepo);
    }

    private Domain.Event.Aggregates.Event CreateReadyEvent()
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        ev.EditTitle("Rollspel för alla");
        ev.EditDescription("En spännande session.");
        ev.SetRegistrationType(RegistrationType.PreRegistration);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        return ev;
    }

    [Fact]
    public async Task Handle_ValidCommand_StatusBecomesUnderReview()
    {
        var ev = CreateReadyEvent();

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.UnderReview, ev.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesSubmittedEvent()
    {
        var ev = CreateReadyEvent();
        ev.ClearDomainEvents();

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Single(ev.DomainEvents.OfType<Domain.Event.Events.EventSubmittedForReview>());
    }

    [Fact]
    public async Task Handle_MissingTitle_Throws()
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        ev.EditDescription("Beskrivning");
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<EventTitleRequiredException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyUnderReview_Throws()
    {
        var ev = CreateReadyEvent();
        ev.SubmitForReview();
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<EventAlreadyUnderReviewException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }
}
