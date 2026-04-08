namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct StaffApplicationId(Guid Value)
{
    public static StaffApplicationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
