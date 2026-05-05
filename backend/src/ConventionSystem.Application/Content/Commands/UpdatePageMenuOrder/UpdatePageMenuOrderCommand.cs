using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.UpdatePageMenuOrder;

public sealed record UpdatePageMenuOrderCommand(Guid PageId, int MenuSortOrder) : ICommand;
