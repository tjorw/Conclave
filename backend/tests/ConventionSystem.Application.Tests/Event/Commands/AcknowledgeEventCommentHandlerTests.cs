using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AcknowledgeEventComment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AcknowledgeEventCommentHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AcknowledgeEventCommentHandler _handler;

    public AcknowledgeEventCommentHandlerTests()
    {
        _handler = new AcknowledgeEventCommentHandler(_eventRepo, _currentUser);
    }

    private Domain.Event.Aggregates.Event SetupRespondedEvent(PersonId organiserId, out Domain.Event.Entities.EventComment comment)
    {
        var adminId = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), organiserId);
        ev.EditTitle("Titel");
        ev.EditDescription("Beskrivning");
        ev.Approve(adminId);
        comment = ev.AddOrganiserComment(organiserId, "Önskar ändring.");
        ev.RespondToComment(comment.Id, adminId, "Fixat.");
        _eventRepo.GetByIdWithCommentsAndCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        return ev;
    }

    [Fact]
    public async Task Handle_OrganiserAcknowledges_CommentBecomesAcknowledged()
    {
        var organiserId = PersonId.New();
        var ev = SetupRespondedEvent(organiserId, out var comment);
        _currentUser.PersonId.Returns(organiserId);

        await _handler.Handle(new AcknowledgeEventCommentCommand(ev.Id.Value, comment.Id.Value), default);

        Assert.Equal(EventCommentStatus.Acknowledged, ev.Comments[0].Status);
    }

    [Fact]
    public async Task Handle_NonOrganiser_Throws()
    {
        var organiserId = PersonId.New();
        var ev = SetupRespondedEvent(organiserId, out var comment);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new AcknowledgeEventCommentCommand(ev.Id.Value, comment.Id.Value), default));
    }

    [Fact]
    public async Task Handle_CommentNotYetResponded_Throws()
    {
        var organiserId = PersonId.New();
        var adminId = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), organiserId);
        ev.EditTitle("Titel");
        ev.EditDescription("Beskrivning");
        ev.Approve(adminId);
        var comment = ev.AddOrganiserComment(organiserId, "Önskar ändring."); // Status: New
        _eventRepo.GetByIdWithCommentsAndCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(organiserId);

        await Assert.ThrowsAsync<EventCommentNotRespondedException>(() =>
            _handler.Handle(new AcknowledgeEventCommentCommand(ev.Id.Value, comment.Id.Value), default));
    }
}
