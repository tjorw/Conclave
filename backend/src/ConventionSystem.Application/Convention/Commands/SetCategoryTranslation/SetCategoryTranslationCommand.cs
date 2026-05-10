using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Commands.SetCategoryTranslation;

public sealed record SetCategoryTranslationCommand(
    Guid EditionId,
    Guid CategoryId,
    string Locale,
    string Name) : ICommand;
