using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodeRedemptions;

public sealed class ListPromotionCodeRedemptionsHandler(IPromotionCodeRepository promotionCodeRepository)
    : IRequestHandler<ListPromotionCodeRedemptionsQuery, IReadOnlyList<PromotionCodeRedemptionHistoryDto>>
{
    public Task<IReadOnlyList<PromotionCodeRedemptionHistoryDto>> Handle(ListPromotionCodeRedemptionsQuery query, CancellationToken ct)
        => promotionCodeRepository.ListRedemptionsAsync(new PromotionCodeId(query.PromotionCodeId), ct);
}
