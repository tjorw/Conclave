using MediatR;

namespace ConventionSystem.Application.Registration.Commands.UpdateTicketType;

public sealed record UpdateTicketTypeCommand(
    Guid TicketTypeId,
    string Name,
    int Price,
    bool IsSellable,
    bool IsPubliclyVisible) : IRequest;
