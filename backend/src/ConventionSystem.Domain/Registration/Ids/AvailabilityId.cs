namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct AvailabilityId(Guid Value)
{
    public static AvailabilityId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
