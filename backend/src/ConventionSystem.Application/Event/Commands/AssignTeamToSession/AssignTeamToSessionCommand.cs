using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.AssignTeamToSession;

public sealed record AssignTeamToSessionCommand(
    Guid EventId,
    Guid SessionId,
    Guid TeamEventRegistrationId) : ICommand;
