namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct PersonId(Guid Value)
{
    public static PersonId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
