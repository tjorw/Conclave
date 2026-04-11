using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RegisterForSession;

public sealed record RegisterForSessionCommand(
    Guid SessionId,
    Guid PersonId,
    Guid TicketId) : IRequest<Guid>;
