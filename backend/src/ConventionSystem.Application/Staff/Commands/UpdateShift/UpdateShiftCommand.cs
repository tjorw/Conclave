namespace ConventionSystem.Application.Staff.Commands.UpdateShift;

public sealed record UpdateShiftCommand(
    Guid ShiftId,
    Guid StationId,
    Guid ResponsibleId,
    DateTime StartTime,
    DateTime EndTime,
    int MinPersons,
    int MaxPersons) : ICommand;
