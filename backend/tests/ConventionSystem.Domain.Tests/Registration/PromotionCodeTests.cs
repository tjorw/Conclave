using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class PromotionCodeTests
{
    [Fact]
    public void Constructor_NormalizesCodeToUppercase()
    {
        var code = new PromotionCode(
            PromotionCodeId.New(),
            EditionId.New(),
            " spring25 ",
            "Vårkampanj",
            PromotionDiscountType.Fixed,
            2500,
            null,
            null,
            null,
            null,
            PersonId.New());

        Assert.Equal("SPRING25", code.Code);
        Assert.Single(code.DomainEvents.OfType<PromotionCodeCreated>());
    }

    [Fact]
    public void Redeem_IncrementsRedemptionCountAndCreatesEvent()
    {
        var promotion = new PromotionCode(
            PromotionCodeId.New(),
            EditionId.New(),
            "SAVE10",
            "10% rabatt",
            PromotionDiscountType.Percentage,
            10,
            null,
            null,
            null,
            null,
            PersonId.New());

        var redemption = promotion.Redeem(TicketId.New(), PersonId.New(), TicketTypeId.New(), 10000, DateTimeOffset.UtcNow);

        Assert.Equal(1, promotion.RedemptionCount);
        Assert.Equal(1000, redemption.DiscountApplied);
        Assert.Equal(9000, redemption.FinalPrice);
        Assert.Single(promotion.DomainEvents.OfType<PromotionCodeRedeemed>());
    }

    [Fact]
    public void Redeem_InactiveCode_Throws()
    {
        var promotion = new PromotionCode(
            PromotionCodeId.New(),
            EditionId.New(),
            "OFF",
            "Inaktiv",
            PromotionDiscountType.Fixed,
            1000,
            null,
            null,
            null,
            null,
            PersonId.New());
        promotion.Deactivate(PersonId.New());

        Assert.Throws<PromotionCodeInactiveException>(() =>
            promotion.Redeem(TicketId.New(), PersonId.New(), TicketTypeId.New(), 5000, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Deactivate_RaisesEvent()
    {
        var promotion = new PromotionCode(
            PromotionCodeId.New(),
            EditionId.New(),
            "STOP",
            "Stopp",
            PromotionDiscountType.Fixed,
            100,
            null,
            null,
            null,
            null,
            PersonId.New());

        promotion.Deactivate(PersonId.New());

        Assert.False(promotion.IsActive);
        Assert.Single(promotion.DomainEvents.OfType<PromotionCodeDeactivated>());
    }
}
