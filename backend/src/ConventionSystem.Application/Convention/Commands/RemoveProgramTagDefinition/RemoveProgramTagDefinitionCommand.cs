namespace ConventionSystem.Application.Convention.Commands.RemoveProgramTagDefinition;

public sealed record RemoveProgramTagDefinitionCommand(
    Guid EditionId,
    string Name) : ICommand;
