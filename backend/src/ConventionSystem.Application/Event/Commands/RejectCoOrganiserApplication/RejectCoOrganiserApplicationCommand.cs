namespace ConventionSystem.Application.Event.Commands.RejectCoOrganiserApplication;

public sealed record RejectCoOrganiserApplicationCommand(
    Guid EventId,
    Guid ApplicationId,
    string? Comment) : ICommand;
