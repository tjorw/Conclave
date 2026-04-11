using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.RemoveSessionRequest;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class RemoveSessionRequestHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly RemoveSessionRequestHandler _handler;

    public RemoveSessionRequestHandlerTests()
    {
        _handler = new RemoveSessionRequestHandler(_eventRepo);
    }

    private Domain.Event.Aggregates.Event CreateDraftEventWithRequest(out Guid requestId)
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        var request = ev.GetDraftVersion().AddSessionRequest("Beskrivning", 120, 4, StartType.FixedTime);
        requestId = request.Id.Value;
        _eventRepo.GetByIdWithDraftVersionAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        return ev;
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesRequest()
    {
        var ev = CreateDraftEventWithRequest(out var requestId);

        await _handler.Handle(new RemoveSessionRequestCommand(ev.Id.Value, requestId), default);

        Assert.Empty(ev.GetDraftVersion().SessionRequests);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var ev = CreateDraftEventWithRequest(out var requestId);

        await _handler.Handle(new RemoveSessionRequestCommand(ev.Id.Value, requestId), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentRequest_Throws()
    {
        var ev = CreateDraftEventWithRequest(out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new RemoveSessionRequestCommand(ev.Id.Value, Guid.NewGuid()), default));
    }
}
