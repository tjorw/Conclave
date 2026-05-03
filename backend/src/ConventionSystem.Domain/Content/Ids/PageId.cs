namespace ConventionSystem.Domain.Content.Ids;

public readonly record struct PageId(Guid Value)
{
    public static PageId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
