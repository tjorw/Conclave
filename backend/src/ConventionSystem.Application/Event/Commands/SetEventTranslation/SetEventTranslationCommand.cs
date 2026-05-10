using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.SetEventTranslation;

public sealed record SetEventTranslationCommand(
    Guid EventId,
    string Locale,
    string Title,
    string Description) : ICommand;
