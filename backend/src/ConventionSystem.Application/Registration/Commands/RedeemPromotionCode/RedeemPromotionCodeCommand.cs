using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RedeemPromotionCode;

public sealed record RedeemPromotionCodeCommand(
    Guid TicketId,
    string Code) : IRequest<RedeemPromotionCodeResult>;

public sealed record RedeemPromotionCodeResult(
    Guid TicketId,
    Guid PromotionCodeId,
    int DiscountApplied,
    int FinalPrice,
    string TicketStatus);
