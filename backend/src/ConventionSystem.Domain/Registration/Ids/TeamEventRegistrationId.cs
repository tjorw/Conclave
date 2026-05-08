namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct TeamEventRegistrationId(Guid Value)
{
    public static TeamEventRegistrationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
