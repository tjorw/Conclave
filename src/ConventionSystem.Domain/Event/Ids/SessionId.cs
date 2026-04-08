namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
