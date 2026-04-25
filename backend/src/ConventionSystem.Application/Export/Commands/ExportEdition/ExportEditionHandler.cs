using System.Text;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Export.Abstractions;

namespace ConventionSystem.Application.Export.Commands.ExportEdition;

public sealed class ExportEditionHandler(IEditionExportReadService exportReadService)
    : ICommandHandler<ExportEditionCommand, ExportEditionResult>
{
    public async Task<ExportEditionResult> Handle(ExportEditionCommand command, CancellationToken ct)
    {
        var document = await exportReadService.BuildDocumentAsync(
            command.EditionId,
            command.IncludeEvents,
            command.IncludeTicketTypes,
            ct) ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        return new ExportEditionResult(CreateFileName(document.Name), document);
    }

    private static string CreateFileName(string editionName)
    {
        var builder = new StringBuilder();
        foreach (var ch in editionName.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "edition";

        return $"{slug}-export.json";
    }
}
