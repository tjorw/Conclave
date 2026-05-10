using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Commands.SetProgramTagTranslation;

public sealed record SetProgramTagTranslationCommand(
    Guid EditionId,
    string TagName,
    string Locale,
    string TranslatedName) : ICommand;
