using ConventionSystem.Domain.Registration.Enums;

namespace ConventionSystem.Application.Registration.Commands.CreateTicketType;

public sealed record CreateTicketTypeCommand(
    Guid EditionId,
    string Name,
    int Price,
    TicketTypeCategory Category,
    IReadOnlyList<DateOnly>? ValidDays = null,
    Guid[]? AllowedCategories = null) : ICommand<Guid>;
