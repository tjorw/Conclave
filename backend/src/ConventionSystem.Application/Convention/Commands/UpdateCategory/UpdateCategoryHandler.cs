using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateCategoryCommand>
{
    protected override async Task ExecuteAsync(UpdateCategoryCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        edition.UpdateCategory(
            new CategoryId(command.CategoryId),
            command.Name,
            command.Description,
            new PersonId(command.ResponsibleId));

        await editionRepository.SaveAsync(ct);
    }
}
