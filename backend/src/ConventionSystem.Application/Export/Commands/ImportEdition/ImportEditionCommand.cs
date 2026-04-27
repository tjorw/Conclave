using ConventionSystem.Application.Export.Contracts;

namespace ConventionSystem.Application.Export.Commands.ImportEdition;

public sealed record ImportEditionCommand(
    Guid ConventionId,
    string Name,
    DateOnly StartDate,
    EditionExportDocument Document) : ICommand<ImportEditionResult>;

public sealed record ImportEditionResult(
    Guid EditionId,
    IReadOnlyList<ImportWarning> Warnings);

public sealed record ImportWarning(
    string Code,
    string Message);
