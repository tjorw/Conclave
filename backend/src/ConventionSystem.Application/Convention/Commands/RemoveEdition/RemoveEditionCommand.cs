namespace ConventionSystem.Application.Convention.Commands.RemoveEdition;

public sealed record RemoveEditionCommand(Guid EditionId) : ICommand;
