namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct EventId(Guid Value)
{
    public static EventId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
