namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct EditionId(Guid Value)
{
    public static EditionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
