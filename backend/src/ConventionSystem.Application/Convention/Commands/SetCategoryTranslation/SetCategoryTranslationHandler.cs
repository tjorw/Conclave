using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.SetCategoryTranslation;

public sealed class SetCategoryTranslationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetCategoryTranslationCommand>
{
    protected override async Task ExecuteAsync(SetCategoryTranslationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);

        var edition = await editionRepository.GetByIdWithCategoriesAndTranslationsAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Utföraren är inte administratör för denna konvention.");

        edition.SetCategoryTranslation(
            new CategoryId(command.CategoryId),
            command.Locale,
            command.Name);

        await editionRepository.SaveAsync(ct);
    }
}
