namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct VisitorRegistrationId(Guid Value)
{
    public static VisitorRegistrationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
