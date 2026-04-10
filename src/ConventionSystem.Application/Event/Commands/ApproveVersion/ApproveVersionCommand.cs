using MediatR;

namespace ConventionSystem.Application.Event.Commands.ApproveVersion;

public sealed record ApproveVersionCommand(
    Guid EventId,
    Guid PerformedById) : IRequest;
