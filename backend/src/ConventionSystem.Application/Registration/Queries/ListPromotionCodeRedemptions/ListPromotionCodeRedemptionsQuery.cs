using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodeRedemptions;

public sealed record ListPromotionCodeRedemptionsQuery(Guid PromotionCodeId)
    : IQuery<IReadOnlyList<PromotionCodeRedemptionHistoryDto>>;
