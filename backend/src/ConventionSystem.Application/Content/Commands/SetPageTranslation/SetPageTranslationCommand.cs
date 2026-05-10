using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.SetPageTranslation;

public sealed record SetPageTranslationCommand(
    Guid PageId,
    string Locale,
    string Title,
    string Content) : ICommand;
