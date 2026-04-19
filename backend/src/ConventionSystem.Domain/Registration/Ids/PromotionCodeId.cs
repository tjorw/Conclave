namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct PromotionCodeId(Guid Value)
{
    public static PromotionCodeId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
