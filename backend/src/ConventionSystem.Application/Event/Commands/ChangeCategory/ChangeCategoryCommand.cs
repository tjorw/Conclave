
namespace ConventionSystem.Application.Event.Commands.ChangeCategory;

public sealed record ChangeCategoryCommand(Guid EventId, Guid CategoryId) : ICommand;
