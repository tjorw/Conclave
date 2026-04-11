namespace ConventionSystem.Domain.Staff.Ids;

public readonly record struct ShiftId(Guid Value)
{
    public static ShiftId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
