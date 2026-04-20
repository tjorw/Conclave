using ConventionSystem.Domain.Event.Enums;

namespace ConventionSystem.Application.Event.Commands.AddSessionRequest;

public sealed record AddSessionRequestCommand(
    Guid EventId,
    string Description,
    int DurationMinutes,
    int Seats,
    StartType StartType) : ICommand<Guid>;
