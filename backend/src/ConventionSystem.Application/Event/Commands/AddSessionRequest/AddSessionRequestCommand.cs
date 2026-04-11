using ConventionSystem.Domain.Event.Enums;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddSessionRequest;

public sealed record AddSessionRequestCommand(
    Guid EventId,
    string Description,
    int DurationMinutes,
    int Seats,
    StartType StartType) : IRequest<Guid>;
