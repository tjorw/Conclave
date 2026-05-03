using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.UnpublishPage;

public sealed record UnpublishPageCommand(Guid PageId) : ICommand;
