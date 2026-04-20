using ConventionSystem.Domain.Registration.Enums;

namespace ConventionSystem.Application.Registration.Commands.UpdateTicketType;

public sealed record UpdateTicketTypeCommand(
    Guid TicketTypeId,
    string Name,
    int Price,
    TicketTypeCategory Category,
    IReadOnlyList<DateOnly>? ValidDays = null,
    Guid[]? AllowedCategories = null) : ICommand;
