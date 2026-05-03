using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.UpdatePage;

public sealed record UpdatePageCommand(Guid PageId, string Slug, string Title, string Content, Guid? EditionId) : ICommand;
