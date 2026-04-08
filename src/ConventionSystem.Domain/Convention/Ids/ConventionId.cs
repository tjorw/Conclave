namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct ConventionId(Guid Value)
{
    public static ConventionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
