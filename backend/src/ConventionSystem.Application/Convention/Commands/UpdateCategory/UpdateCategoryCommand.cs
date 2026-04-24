
namespace ConventionSystem.Application.Convention.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid EditionId,
    Guid CategoryId,
    string Name,
    string? OrganizerInstructions,
    string? PublicDescription,
    Guid ResponsibleId) : ICommand;
