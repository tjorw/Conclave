using ConventionSystem.Domain.Registration.Enums;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CreateTicketType;

public sealed record CreateTicketTypeCommand(
    Guid EditionId,
    string Name,
    int Price,
    TicketTypeCategory Category,
    IReadOnlyList<DateOnly>? ValidDays = null,
    Guid[]? AllowedCategories = null) : IRequest<Guid>;
