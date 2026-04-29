using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.SetCoOrganiserCount;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class SetCoOrganiserCountHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetCoOrganiserCountHandler _handler;

    public SetCoOrganiserCountHandlerTests()
    {
        _handler = new SetCoOrganiserCountHandler(_eventRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev, PersonId leadId) Setup()
    {
        var leadId = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), leadId);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(leadId);
        return (ev, leadId);
    }

    [Fact]
    public async Task Handle_LeadOrganiser_SetsCount()
    {
        var (ev, _) = Setup();

        await _handler.Handle(new SetCoOrganiserCountCommand(ev.Id.Value, 3), default);

        Assert.Equal(3, ev.CoOrganiserCount);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotLeadOrganiser_ThrowsForbiddenException()
    {
        var (ev, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new SetCoOrganiserCountCommand(ev.Id.Value, 3), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsResourceNotFoundException()
    {
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>()).Returns((Domain.Event.Aggregates.Event?)null);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new SetCoOrganiserCountCommand(Guid.NewGuid(), 3), default));
    }
}
