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
    bool IsSellable,
    bool IsPubliclyVisible);

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
    Guid TicketId);

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

public record MyStaffApplicationDto(
    Guid Id,
    string Status);
