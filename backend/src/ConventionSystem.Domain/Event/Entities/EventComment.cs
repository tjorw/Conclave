using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class EventComment : Entity<EventCommentId>
{
    public EventId EventId { get; private set; }
    public PersonId AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public EventCommentStatus Status { get; private set; }
    public bool RequiresHandling { get; private set; }
    public string? HandlingComment { get; private set; }
    public PersonId? HandledById { get; private set; }
    public DateTimeOffset? HandledAt { get; private set; }
    public PersonId? AcknowledgedById { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private EventComment() { }

    internal EventComment(EventCommentId id, EventId eventId, PersonId authorId, string text, bool requiresHandling)
        : base(id)
    {
        EventId = eventId;
        AuthorId = authorId;
        Text = text;
        RequiresHandling = requiresHandling;
        Status = requiresHandling ? EventCommentStatus.New : EventCommentStatus.Responded;
        CreatedAt = DateTimeOffset.UtcNow;

        if (!requiresHandling)
        {
            HandledById = authorId;
            HandledAt = CreatedAt;
            HandlingComment = text;
        }
    }

    public void Respond(PersonId handledById, string handlingComment)
    {
        if (!RequiresHandling)
            throw new EventCommentDoesNotRequireHandlingException();
        if (Status is EventCommentStatus.Responded or EventCommentStatus.Acknowledged)
            throw new EventCommentAlreadyRespondedException();
        if (string.IsNullOrWhiteSpace(handlingComment))
            throw new EventCommentResponseRequiredException();

        Status = EventCommentStatus.Responded;
        HandlingComment = handlingComment;
        HandledById = handledById;
        HandledAt = DateTimeOffset.UtcNow;
    }

    public void Acknowledge(PersonId acknowledgedById)
    {
        if (!RequiresHandling)
            throw new EventCommentDoesNotRequireHandlingException();
        if (Status == EventCommentStatus.Acknowledged)
            throw new EventCommentAlreadyAcknowledgedException();
        if (Status != EventCommentStatus.Responded)
            throw new EventCommentNotRespondedException();

        Status = EventCommentStatus.Acknowledged;
        AcknowledgedById = acknowledgedById;
        AcknowledgedAt = DateTimeOffset.UtcNow;
    }
}
