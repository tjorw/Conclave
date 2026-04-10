using MediatR;

namespace ConventionSystem.Application.Event.Commands.DeactivateSession;

public sealed record DeactivateSessionCommand(
    Guid EventId,
    Guid SessionId) : IRequest;
