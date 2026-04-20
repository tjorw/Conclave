
namespace ConventionSystem.Application.Event.Commands.RejectVersion;

public sealed record RejectVersionCommand(
    Guid EventId,
    string Comment) : ICommand;
