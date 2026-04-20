
namespace ConventionSystem.Application.Convention.Commands.SetActiveEdition;

public sealed record SetActiveEditionCommand(Guid EditionId) : ICommand;
