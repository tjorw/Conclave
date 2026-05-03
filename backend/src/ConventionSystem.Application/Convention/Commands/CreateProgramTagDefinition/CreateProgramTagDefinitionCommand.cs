namespace ConventionSystem.Application.Convention.Commands.CreateProgramTagDefinition;

public sealed record CreateProgramTagDefinitionCommand(
    Guid EditionId,
    string Name) : ICommand;
