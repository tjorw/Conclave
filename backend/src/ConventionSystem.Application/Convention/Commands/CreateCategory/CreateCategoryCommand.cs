
namespace ConventionSystem.Application.Convention.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    Guid EditionId,
    string Name,
    string? OrganizerInstructions,
    string? PublicDescription,
    Guid ResponsibleId) : ICommand<Guid>;
