using ConventionSystem.Domain.Event.Enums;

namespace ConventionSystem.Application.Event.Commands.ScheduleSession;

public sealed record ScheduleSessionCommand(
    Guid EventId,
    Guid VenueId,
    DateTime StartTime,
    DateTime EndTime,
    int MaxSeats,
    StartType StartType) : ICommand<Guid>;
