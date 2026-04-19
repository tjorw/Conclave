namespace ConventionSystem.Application.Registration.Queries;

public record EditionVisitorDto(
    Guid PersonId,
    string PersonName,
    string Email,
    string? Phone);

public record TicketTypeAdminDto(
    Guid Id,
    string Name,
    int Price,
    string Category,
    IReadOnlyList<DateOnly>? ValidDays,
    Guid[]? AllowedCategories);

public record VisitorTicketTypeDto(
    Guid Id,
    string Name,
    int Price);

public record VisitorRegistrationAdminDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? TicketTypeName,
    string Status,
    DateTimeOffset RegisteredAt,
    string? PaymentReference);

public record MyVisitorRegistrationDto(
    Guid Id,
    string Status,
    string? TicketTypeName,
    Guid TicketId,
    int? TicketPrice);

public record MySessionRegistrationSummaryDto(
    Guid Id,
    Guid SessionId,
    string EventTitle,
    DateTime Start,
    DateTime End,
    string VenueName,
    string Status);

public record MyWatchedSessionSummaryDto(
    Guid SessionId,
    string EventTitle,
    DateTime Start,
    DateTime End,
    string VenueName,
    DateTimeOffset CreatedAt);

public record MyOrganiserSessionSummaryDto(
    Guid SessionId,
    string EventTitle,
    DateTime Start,
    DateTime End,
    string VenueName);

public record MyAssignedShiftSummaryDto(
    Guid ShiftId,
    string StationName,
    DateTime Start,
    DateTime End);

public record MyStaffApplicationDto(
    Guid Id,
    string Status);

public record PromotionCodeAdminDto(
    Guid Id,
    string Code,
    string Description,
    string DiscountType,
    int DiscountValue,
    bool IsActive,
    int RedemptionCount,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    Guid[]? AllowedTicketTypeIds);

public record PromotionCodeRedemptionHistoryDto(
    Guid Id,
    Guid PersonId,
    Guid TicketId,
    DateTimeOffset RedeemedAt,
    int DiscountApplied,
    int FinalPrice);
