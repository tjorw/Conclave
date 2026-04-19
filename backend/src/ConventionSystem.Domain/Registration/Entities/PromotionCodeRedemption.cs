using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class PromotionCodeRedemption : Entity<PromotionCodeRedemptionId>
{
    public PromotionCodeId PromotionCodeId { get; private set; }
    public TicketId TicketId { get; private set; }
    public PersonId PersonId { get; private set; }
    public TicketTypeId TicketTypeId { get; private set; }
    public int DiscountApplied { get; private set; }
    public int FinalPrice { get; private set; }
    public DateTimeOffset RedeemedAt { get; private set; }

    private PromotionCodeRedemption() { }

    internal PromotionCodeRedemption(
        PromotionCodeRedemptionId id,
        PromotionCodeId promotionCodeId,
        TicketId ticketId,
        PersonId personId,
        TicketTypeId ticketTypeId,
        int discountApplied,
        int finalPrice,
        DateTimeOffset redeemedAt)
        : base(id)
    {
        PromotionCodeId = promotionCodeId;
        TicketId = ticketId;
        PersonId = personId;
        TicketTypeId = ticketTypeId;
        DiscountApplied = discountApplied;
        FinalPrice = finalPrice;
        RedeemedAt = redeemedAt;
    }
}
