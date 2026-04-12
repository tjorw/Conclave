using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class EventComment : Entity<EventCommentId>
{
    public EventId EventId { get; private set; }
    public PersonId AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private EventComment() { }

    internal EventComment(EventCommentId id, EventId eventId, PersonId authorId, string text)
        : base(id)
    {
        EventId = eventId;
        AuthorId = authorId;
        Text = text;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
