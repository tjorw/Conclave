using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.SetProgramTagTranslation;

public sealed class SetProgramTagTranslationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetProgramTagTranslationCommand>
{
    protected override async Task ExecuteAsync(SetProgramTagTranslationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);

        var edition = await editionRepository.GetByIdWithProgramTagTranslationsAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Utföraren är inte administratör för denna konvention.");

        edition.SetProgramTagTranslation(
            command.TagName,
            command.Locale,
            command.TranslatedName);

        await editionRepository.SaveAsync(ct);
    }
}
