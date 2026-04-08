namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct SessionRequestId(Guid Value)
{
    public static SessionRequestId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
