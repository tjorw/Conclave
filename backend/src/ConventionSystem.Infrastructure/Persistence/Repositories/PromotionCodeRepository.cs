using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class PromotionCodeRepository(ConventionDbContext db) : IPromotionCodeRepository
{
    public Task<PromotionCode?> GetByIdAsync(PromotionCodeId id, CancellationToken ct = default)
        => db.PromotionCodes
            .Include(p => p.Redemptions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PromotionCode?> GetByEditionAndCodeAsync(EditionId editionId, string code, CancellationToken ct = default)
        => db.PromotionCodes
            .Include(p => p.Redemptions)
            .FirstOrDefaultAsync(
                p => p.EditionId == editionId && p.Code == code,
                ct);

    public Task<bool> ExistsByEditionAndCodeAsync(EditionId editionId, string code, CancellationToken ct = default)
        => db.PromotionCodes.AnyAsync(p => p.EditionId == editionId && p.Code == code, ct);

    public async Task<IReadOnlyList<PromotionCodeAdminDto>> ListByEditionAsync(EditionId editionId, CancellationToken ct = default)
    {
        return await db.PromotionCodes
            .Where(p => p.EditionId == editionId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PromotionCodeAdminDto(
                p.Id.Value,
                p.Code,
                p.Description,
                p.DiscountType.ToString(),
                p.DiscountValue,
                p.IsActive,
                p.Redemptions.Count,
                p.MaxRedemptions,
                p.ValidFrom,
                p.ValidUntil,
                p.AllowedTicketTypeIds))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PromotionCodeRedemptionHistoryDto>> ListRedemptionsAsync(PromotionCodeId promotionCodeId, CancellationToken ct = default)
    {
        return await db.Set<Domain.Registration.Entities.PromotionCodeRedemption>()
            .Where(r => r.PromotionCodeId == promotionCodeId)
            .OrderByDescending(r => r.RedeemedAt)
            .Select(r => new PromotionCodeRedemptionHistoryDto(
                r.Id.Value,
                r.PersonId.Value,
                r.TicketId.Value,
                r.RedeemedAt,
                r.DiscountApplied,
                r.FinalPrice))
            .ToListAsync(ct);
    }

    public async Task AddAndSaveAsync(PromotionCode promotionCode, CancellationToken ct = default)
    {
        db.PromotionCodes.Add(promotionCode);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
