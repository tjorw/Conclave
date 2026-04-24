namespace ConventionSystem.Application.Event.Commands.CancelCoOrganiserApplication;

public sealed record CancelCoOrganiserApplicationCommand(
    Guid EventId,
    Guid ApplicationId) : ICommand;
