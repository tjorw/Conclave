using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodes;

public sealed class ListPromotionCodesHandler(IPromotionCodeRepository promotionCodeRepository)
    : IRequestHandler<ListPromotionCodesQuery, IReadOnlyList<PromotionCodeAdminDto>>
{
    public Task<IReadOnlyList<PromotionCodeAdminDto>> Handle(ListPromotionCodesQuery query, CancellationToken ct)
        => promotionCodeRepository.ListByEditionAsync(new EditionId(query.EditionId), ct);
}
