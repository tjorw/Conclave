using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Aggregates;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public class EventCommentWorkflowTests
{
    private static Domain.Event.Aggregates.Event CreatePublishedEvent(PersonId organiserId)
    {
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), organiserId);
        ev.EditTitle("Titel");
        ev.EditDescription("Beskrivning");
        ev.Approve(PersonId.New());
        return ev;
    }

    [Fact]
    public void AddOrganiserComment_WhenPublished_AddsNewComment()
    {
        var organiserId = PersonId.New();
        var ev = CreatePublishedEvent(organiserId);

        var comment = ev.AddOrganiserComment(organiserId, "Kan vi flytta sessionen till senare?");

        Assert.Single(ev.Comments);
        Assert.Equal(EventCommentStatus.New, comment.Status);
        Assert.True(comment.RequiresHandling);
    }

    [Fact]
    public void RespondToComment_ThenAcknowledge_TransitionsStatus()
    {
        var organiserId = PersonId.New();
        var adminId = PersonId.New();
        var ev = CreatePublishedEvent(organiserId);
        var comment = ev.AddOrganiserComment(organiserId, "Önskar justering.");

        ev.RespondToComment(comment.Id, adminId, "Vi har flyttat till större sal kl 19:00.");
        ev.AcknowledgeComment(comment.Id, organiserId);

        Assert.Equal(EventCommentStatus.Acknowledged, comment.Status);
        Assert.NotNull(comment.HandledAt);
        Assert.NotNull(comment.AcknowledgedAt);
    }

    [Fact]
    public void AcknowledgeComment_ByOtherPerson_Throws()
    {
        var organiserId = PersonId.New();
        var adminId = PersonId.New();
        var outsiderId = PersonId.New();
        var ev = CreatePublishedEvent(organiserId);
        var comment = ev.AddOrganiserComment(organiserId, "Önskar justering.");
        ev.RespondToComment(comment.Id, adminId, "Svar");

        Assert.Throws<EventCommentAcknowledgeMustBeDoneByAuthorException>(
            () => ev.AcknowledgeComment(comment.Id, outsiderId));
    }

    [Fact]
    public void AddOrganiserComment_WhenNotPublished_Throws()
    {
        var organiserId = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), EditionId.New(), CategoryId.New(), organiserId);

        Assert.Throws<EventNotPublishedException>(
            () => ev.AddOrganiserComment(organiserId, "Kommentar"));
    }
}
