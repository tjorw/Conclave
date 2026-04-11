namespace ConventionSystem.Domain.Staff.Ids;

public readonly record struct StaffAssignmentId(Guid Value)
{
    public static StaffAssignmentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
