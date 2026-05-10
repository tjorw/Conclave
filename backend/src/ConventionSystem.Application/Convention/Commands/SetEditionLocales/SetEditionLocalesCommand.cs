using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Commands.SetEditionLocales;

public sealed record SetEditionLocalesCommand(
    Guid EditionId,
    IReadOnlyList<string> Locales,
    string PrimaryLocale) : ICommand;
