namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct PromotionCodeRedemptionId(Guid Value)
{
    public static PromotionCodeRedemptionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
