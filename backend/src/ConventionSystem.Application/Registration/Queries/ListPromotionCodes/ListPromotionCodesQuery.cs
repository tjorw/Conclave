using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodes;

public sealed record ListPromotionCodesQuery(Guid EditionId)
    : IRequest<IReadOnlyList<PromotionCodeAdminDto>>;
