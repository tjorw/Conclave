using ConventionSystem.Application.Export.Contracts;

namespace ConventionSystem.Application.Export.Commands.ExportEdition;

public sealed record ExportEditionCommand(
    Guid EditionId,
    bool IncludeEvents,
    bool IncludeTicketTypes) : ICommand<ExportEditionResult>;

public sealed record ExportEditionResult(
    string FileName,
    EditionExportDocument Document);
