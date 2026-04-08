namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct SessionRegistrationId(Guid Value)
{
    public static SessionRegistrationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
