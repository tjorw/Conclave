namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
