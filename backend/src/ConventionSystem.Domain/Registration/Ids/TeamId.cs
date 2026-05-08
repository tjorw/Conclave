namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct TeamId(Guid Value)
{
    public static TeamId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
