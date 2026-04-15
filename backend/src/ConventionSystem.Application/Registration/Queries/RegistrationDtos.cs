namespace ConventionSystem.Application.Registration.Queries;

public record EditionVisitorDto(
    Guid PersonId,
    string PersonName,
    string Email,
    string? Phone);

public record MyVisitorRegistrationDto(
    Guid Id,
    string Status,
    string? TicketTypeName);

public record MySessionRegistrationSummaryDto(
    Guid Id,
    Guid SessionId,
    string EventTitle,
    DateTime Start,
    DateTime End,
    string VenueName,
    string Status);

public record MyStaffApplicationDto(
    Guid Id,
    string Status);
