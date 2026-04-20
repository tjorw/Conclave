using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AddSessionRequest;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AddSessionRequestHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly AddSessionRequestHandler _handler;

    public AddSessionRequestHandlerTests()
    {
        _handler = new AddSessionRequestHandler(_eventRepo);
    }

    private Domain.Event.Aggregates.Event CreateDraftEvent()
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        _eventRepo.GetByIdWithSessionRequestsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        return ev;
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSessionRequestId()
    {
        var ev = CreateDraftEvent();

        var id = await _handler.Handle(
            new AddSessionRequestCommand(ev.Id.Value, "Vi vill spela 4 timmar", 240, 6, StartType.FixedTime), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsRequestToEvent()
    {
        var ev = CreateDraftEvent();

        await _handler.Handle(
            new AddSessionRequestCommand(ev.Id.Value, "Beskrivning", 120, 4, StartType.Rolling), default);

        Assert.Single(ev.SessionRequests);
    }

    [Fact]
    public async Task Handle_ZeroDuration_Throws()
    {
        var ev = CreateDraftEvent();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new AddSessionRequestCommand(ev.Id.Value, "Beskrivning", 0, 4, StartType.FixedTime), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdWithSessionRequestsAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(
                new AddSessionRequestCommand(Guid.NewGuid(), "Beskrivning", 120, 4, StartType.FixedTime), default));
    }

    [Fact]
    public async Task Handle_CancelledEvent_Throws()
    {
        var ev = CreateDraftEvent();
        ev.CancelEvent(PersonId.New());

        await Assert.ThrowsAsync<EventIsCancelledAndReadOnlyException>(() =>
            _handler.Handle(
                new AddSessionRequestCommand(ev.Id.Value, "Beskrivning", 120, 4, StartType.FixedTime), default));
    }
}
