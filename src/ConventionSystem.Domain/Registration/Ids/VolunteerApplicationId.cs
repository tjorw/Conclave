namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct VolunteerApplicationId(Guid Value)
{
    public static VolunteerApplicationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
