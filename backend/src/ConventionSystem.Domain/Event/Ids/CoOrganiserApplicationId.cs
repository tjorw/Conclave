namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct CoOrganiserApplicationId(Guid Value)
{
    public static CoOrganiserApplicationId New() => new(Guid.NewGuid());
}
