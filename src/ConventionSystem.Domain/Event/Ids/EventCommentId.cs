namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct EventCommentId(Guid Value)
{
    public static EventCommentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
