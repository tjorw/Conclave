namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct SessionWatchId(Guid Value)
{
    public static SessionWatchId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
