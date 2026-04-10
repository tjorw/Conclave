using MediatR;

namespace ConventionSystem.Application.Event.Commands.RejectVersion;

public sealed record RejectVersionCommand(
    Guid EventId,
    Guid PerformedById,
    string Comment) : IRequest;
