using MediatR;

namespace ConventionSystem.Application.Staff.Commands.CreateShift;

public sealed record CreateShiftCommand(
    Guid StationId,
    Guid ResponsibleId,
    DateTime StartTime,
    DateTime EndTime,
    int MinPersons,
    int MaxPersons) : IRequest<Guid>;
