using ConventionSystem.Domain.Event.Enums;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.UpdateSession;

public sealed record UpdateSessionCommand(
    Guid EventId,
    Guid SessionId,
    Guid VenueId,
    DateTime StartTime,
    DateTime EndTime,
    int MaxSeats,
    StartType StartType) : IRequest;
