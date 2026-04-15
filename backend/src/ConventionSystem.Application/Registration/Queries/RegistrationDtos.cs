namespace ConventionSystem.Application.Registration.Queries;

public record EditionVisitorDto(
    Guid PersonId,
    string PersonName,
    string Email,
    string? Phone);
