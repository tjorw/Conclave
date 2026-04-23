
namespace ConventionSystem.Application.Convention.Commands.CreateEdition;

public sealed record CreateEditionCommand(
    Guid ConventionId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    IReadOnlyList<EditionScheduleDayCommand>? ScheduleDays = null) : ICommand<Guid>;

public sealed record EditionScheduleDayCommand(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime);
