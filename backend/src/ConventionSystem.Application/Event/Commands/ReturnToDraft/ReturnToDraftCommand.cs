using MediatR;

namespace ConventionSystem.Application.Event.Commands.ReturnToDraft;

public sealed record ReturnToDraftCommand(
    Guid EventId) : IRequest;
