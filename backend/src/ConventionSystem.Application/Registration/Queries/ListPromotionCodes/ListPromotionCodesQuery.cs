using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListPromotionCodes;

public sealed record ListPromotionCodesQuery(Guid EditionId)
    : IQuery<IReadOnlyList<PromotionCodeAdminDto>>;
