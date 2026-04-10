using MediatR;

namespace ConventionSystem.Application.Event.Commands.CancelEvent;

public sealed record CancelEventCommand(
    Guid EventId,
    Guid PerformedById) : IRequest;
