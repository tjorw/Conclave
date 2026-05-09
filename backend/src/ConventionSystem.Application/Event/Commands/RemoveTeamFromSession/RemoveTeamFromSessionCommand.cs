using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.RemoveTeamFromSession;

public sealed record RemoveTeamFromSessionCommand(
    Guid EventId,
    Guid SessionId,
    Guid TeamEventRegistrationId) : ICommand;
