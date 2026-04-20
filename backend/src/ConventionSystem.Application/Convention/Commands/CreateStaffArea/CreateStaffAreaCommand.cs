
namespace ConventionSystem.Application.Convention.Commands.CreateStaffArea;

public sealed record CreateStaffAreaCommand(
    Guid EditionId,
    string Name,
    string? Description,
    Guid ResponsibleId) : ICommand<Guid>;
