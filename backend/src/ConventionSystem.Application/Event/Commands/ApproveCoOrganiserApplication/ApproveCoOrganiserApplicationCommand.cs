namespace ConventionSystem.Application.Event.Commands.ApproveCoOrganiserApplication;

public sealed record ApproveCoOrganiserApplicationCommand(
    Guid EventId,
    Guid ApplicationId) : ICommand;
