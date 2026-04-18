using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RegisterManualTicketPayment;

public sealed record RegisterManualTicketPaymentCommand(
    Guid TicketId,
    string? ExternalReference) : IRequest;
