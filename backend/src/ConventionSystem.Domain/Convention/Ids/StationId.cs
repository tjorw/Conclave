namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct StationId(Guid Value)
{
    public static StationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
