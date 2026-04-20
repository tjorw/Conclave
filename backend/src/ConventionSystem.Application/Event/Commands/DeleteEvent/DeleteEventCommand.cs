
namespace ConventionSystem.Application.Event.Commands.DeleteEvent;

public sealed record DeleteEventCommand(Guid EventId) : ICommand;
