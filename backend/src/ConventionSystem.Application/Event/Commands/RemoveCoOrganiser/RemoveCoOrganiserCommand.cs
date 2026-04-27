namespace ConventionSystem.Application.Event.Commands.RemoveCoOrganiser;

public sealed record RemoveCoOrganiserCommand(
    Guid EventId,
    Guid PersonId) : ICommand;
