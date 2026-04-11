using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
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
        var draft = ev.GetDraftVersion();
        draft.EditTitle("Rollspel för alla");
        draft.EditDescription("En spännande session.");
        draft.SetRegistrationType(RegistrationType.PreRegistration);
        _eventRepo.GetByIdWithDraftVersionAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
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
    public async Task Handle_ValidCommand_DraftVersionIsUnderReview()
    {
        var ev = CreateReadyEvent();

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Equal(VersionStatus.UnderReview, ev.GetDraftVersion().Status);
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
        ev.GetDraftVersion().EditDescription("Beskrivning");
        _eventRepo.GetByIdWithDraftVersionAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyUnderReview_Throws()
    {
        var ev = CreateReadyEvent();
        ev.SubmitForReview();
        _eventRepo.GetByIdWithDraftVersionAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }
}
