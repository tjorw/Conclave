
namespace ConventionSystem.Application.Event.Commands.RemoveSessionRequest;

public sealed record RemoveSessionRequestCommand(
    Guid EventId,
    Guid SessionRequestId) : ICommand;
