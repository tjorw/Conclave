using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodeRedemptions;

public sealed record ListPromotionCodeRedemptionsQuery(Guid PromotionCodeId)
    : IRequest<IReadOnlyList<PromotionCodeRedemptionHistoryDto>>;
