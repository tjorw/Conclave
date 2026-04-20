
namespace ConventionSystem.Application.Convention.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid EditionId,
    Guid CategoryId,
    string Name,
    string? Description,
    Guid ResponsibleId) : ICommand;
