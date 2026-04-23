
namespace ConventionSystem.Application.Convention.Commands.UpdateEdition;

public sealed record UpdateEditionCommand(
    Guid EditionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    IReadOnlyList<EditionScheduleDayCommand>? ScheduleDays = null) : ICommand;

public sealed record EditionScheduleDayCommand(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime);
