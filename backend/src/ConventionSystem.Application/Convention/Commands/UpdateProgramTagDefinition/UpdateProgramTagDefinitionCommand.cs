namespace ConventionSystem.Application.Convention.Commands.UpdateProgramTagDefinition;

public sealed record UpdateProgramTagDefinitionCommand(
    Guid EditionId,
    string CurrentName,
    string NewName) : ICommand;
