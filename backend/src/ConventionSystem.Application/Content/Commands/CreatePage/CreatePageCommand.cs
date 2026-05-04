using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.CreatePage;

public sealed record CreatePageCommand(string Slug, string Title, string Content, Guid? EditionId, bool ShowInPublicMenu = false) : ICommand<Guid>;
