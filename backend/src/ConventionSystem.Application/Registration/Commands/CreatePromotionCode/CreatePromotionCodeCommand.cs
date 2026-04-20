using ConventionSystem.Domain.Registration.Enums;

namespace ConventionSystem.Application.Registration.Commands.CreatePromotionCode;

public sealed record CreatePromotionCodeCommand(
    Guid EditionId,
    string Code,
    string Description,
    PromotionDiscountType DiscountType,
    int DiscountValue,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    Guid[]? AllowedTicketTypeIds) : ICommand<Guid>;
