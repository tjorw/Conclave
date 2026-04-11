using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class EventComment : Entity<EventCommentId>
{
    public EventId EventId { get; private set; }
    public EventVersionId? VersionId { get; private set; }
    public PersonId AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private EventComment() { }

    internal EventComment(EventCommentId id, EventId eventId, EventVersionId? versionId, PersonId authorId, string text)
        : base(id)
    {
        EventId = eventId;
        VersionId = versionId;
        AuthorId = authorId;
        Text = text;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
