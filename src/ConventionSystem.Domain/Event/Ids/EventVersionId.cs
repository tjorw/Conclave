namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct EventVersionId(Guid Value)
{
    public static EventVersionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
