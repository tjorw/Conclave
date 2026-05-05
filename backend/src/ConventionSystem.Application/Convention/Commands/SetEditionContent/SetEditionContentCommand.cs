using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Commands.SetEditionContent;

public sealed record SetEditionContentCommand(
    Guid EditionId,
    IReadOnlyList<EditionContentItem> Items) : ICommand;

public sealed record EditionContentItem(string Key, string Value);
