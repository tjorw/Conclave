namespace ConventionSystem.Domain.Convention.Ids;

public readonly record struct StaffAreaId(Guid Value)
{
    public static StaffAreaId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
