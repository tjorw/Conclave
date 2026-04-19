using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class PromotionCode : AggregateRoot
{
    private readonly List<PromotionCodeRedemption> _redemptions = [];

    public PromotionCodeId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PromotionDiscountType DiscountType { get; private set; }
    public int DiscountValue { get; private set; }
    public bool IsActive { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public DateTimeOffset? ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public Guid[]? AllowedTicketTypeIds { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<PromotionCodeRedemption> Redemptions => _redemptions.AsReadOnly();
    public int RedemptionCount => _redemptions.Count;

    private PromotionCode() { }

    public PromotionCode(
        PromotionCodeId id,
        EditionId editionId,
        string code,
        string description,
        PromotionDiscountType discountType,
        int discountValue,
        int? maxRedemptions,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        Guid[]? allowedTicketTypeIds,
        PersonId createdById)
    {
        var normalizedCode = NormalizeCode(code);
        ValidateDiscount(discountType, discountValue);
        ValidateValidityWindow(validFrom, validUntil);

        Id = id;
        EditionId = editionId;
        Code = normalizedCode;
        Description = description?.Trim() ?? string.Empty;
        DiscountType = discountType;
        DiscountValue = discountValue;
        IsActive = true;
        MaxRedemptions = maxRedemptions;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        AllowedTicketTypeIds = allowedTicketTypeIds;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new PromotionCodeCreated(Id, EditionId, Code, createdById, DateTimeOffset.UtcNow));
    }

    public PromotionCodeRedemption Redeem(
        TicketId ticketId,
        PersonId personId,
        TicketTypeId ticketTypeId,
        int ticketPrice,
        DateTimeOffset now)
    {
        if (!IsActive)
            throw new PromotionCodeInactiveException();

        if (MaxRedemptions.HasValue && RedemptionCount >= MaxRedemptions.Value)
            throw new PromotionCodeMaxRedemptionsReachedException();

        if (ValidFrom.HasValue && now < ValidFrom.Value)
            throw new PromotionCodeNotYetValidException();

        if (ValidUntil.HasValue && now > ValidUntil.Value)
            throw new PromotionCodeExpiredException();

        if (AllowedTicketTypeIds is { Length: > 0 } && !AllowedTicketTypeIds.Contains(ticketTypeId.Value))
            throw new PromotionCodeTicketTypeNotAllowedException();

        var discountApplied = CalculateDiscount(ticketPrice);
        var finalPrice = Math.Max(0, ticketPrice - discountApplied);

        var redemption = new PromotionCodeRedemption(
            PromotionCodeRedemptionId.New(),
            Id,
            ticketId,
            personId,
            ticketTypeId,
            discountApplied,
            finalPrice,
            now);

        _redemptions.Add(redemption);

        RaiseDomainEvent(new PromotionCodeRedeemed(Id, ticketId, personId, discountApplied, now));
        return redemption;
    }

    public void Deactivate(PersonId performedById)
    {
        if (!IsActive)
            throw new PromotionCodeInactiveException();

        IsActive = false;
        RaiseDomainEvent(new PromotionCodeDeactivated(Id, performedById, DateTimeOffset.UtcNow));
    }

    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Kampanjkod får inte vara tom.", nameof(code));

        return code.Trim().ToUpperInvariant();
    }

    private static void ValidateDiscount(PromotionDiscountType discountType, int discountValue)
    {
        if (discountValue < 0)
            throw new ArgumentException("Rabattvärde får inte vara negativt.", nameof(discountValue));

        if (discountType == PromotionDiscountType.Percentage && (discountValue < 0 || discountValue > 100))
            throw new PromotionCodeDiscountPercentageOutOfRangeException();
    }

    private static void ValidateValidityWindow(DateTimeOffset? validFrom, DateTimeOffset? validUntil)
    {
        if (validFrom.HasValue && validUntil.HasValue && validFrom > validUntil)
            throw new PromotionCodeInvalidValidityWindowException();
    }

    private int CalculateDiscount(int ticketPrice)
    {
        if (ticketPrice < 0)
            throw new ArgumentException("Biljettpris får inte vara negativt.", nameof(ticketPrice));

        return DiscountType switch
        {
            PromotionDiscountType.Free => ticketPrice,
            PromotionDiscountType.Percentage => ticketPrice * DiscountValue / 100,
            PromotionDiscountType.Fixed => Math.Min(ticketPrice, DiscountValue),
            _ => 0
        };
    }
}
