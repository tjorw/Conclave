using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.DeletePage;

public sealed record DeletePageCommand(Guid PageId) : ICommand;
