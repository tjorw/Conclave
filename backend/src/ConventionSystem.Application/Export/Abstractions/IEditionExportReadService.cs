using ConventionSystem.Application.Export.Contracts;

namespace ConventionSystem.Application.Export.Abstractions;

public interface IEditionExportReadService
{
    Task<EditionExportDocument?> BuildDocumentAsync(
        Guid editionId,
        bool includeEvents,
        bool includeTicketTypes,
        CancellationToken ct = default);
}
