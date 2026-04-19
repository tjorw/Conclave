using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface IPromotionCodeRepository
{
    Task<PromotionCode?> GetByIdAsync(PromotionCodeId id, CancellationToken ct = default);
    Task<PromotionCode?> GetByEditionAndCodeAsync(EditionId editionId, string code, CancellationToken ct = default);
    Task<bool> ExistsByEditionAndCodeAsync(EditionId editionId, string code, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCodeAdminDto>> ListByEditionAsync(EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionCodeRedemptionHistoryDto>> ListRedemptionsAsync(PromotionCodeId promotionCodeId, CancellationToken ct = default);
    Task AddAndSaveAsync(PromotionCode promotionCode, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
