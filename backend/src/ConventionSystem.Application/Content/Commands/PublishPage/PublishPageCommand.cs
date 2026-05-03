using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.PublishPage;

public sealed record PublishPageCommand(Guid PageId) : ICommand;
