namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct VenueId(Guid Value)
{
    public static VenueId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
