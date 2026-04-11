using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.EditEventDraft;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class EditEventDraftHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly EditEventDraftHandler _handler;

    public EditEventDraftHandlerTests()
    {
        _handler = new EditEventDraftHandler(_eventRepo);
    }

    private Domain.Event.Aggregates.Event CreateDraftEvent()
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        _eventRepo.GetByIdWithDraftVersionAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        return ev;
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesDraft()
    {
        var ev = CreateDraftEvent();

        await _handler.Handle(
            new EditEventDraftCommand(ev.Id.Value, "Rollspel för nybörjare",
                "En beskrivning", RegistrationType.PreRegistration, null), default);

        var draft = ev.GetDraftVersion();
        Assert.Equal("Rollspel för nybörjare", draft.Title);
        Assert.Equal("En beskrivning", draft.Description);
        Assert.Equal(RegistrationType.PreRegistration, draft.RegistrationType);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var ev = CreateDraftEvent();

        await _handler.Handle(
            new EditEventDraftCommand(ev.Id.Value, "Titel", "Beskrivning", RegistrationType.DropIn, "Öppen dörrpolicy"), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyTitle_Throws()
    {
        var ev = CreateDraftEvent();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(ev.Id.Value, "  ", "Beskrivning", RegistrationType.PreRegistration, null), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdWithDraftVersionAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(Guid.NewGuid(), "Titel", "Beskrivning", RegistrationType.PreRegistration, null), default));
    }
}
