using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
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

        var context = await EditionContextLoader.LoadWithCategoriesForConventionCommandAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utföraren är inte administratör för denna konvention.");

        context.Edition.UpdateCategory(
            new CategoryId(command.CategoryId),
            command.Name,
            command.OrganizerInstructions,
            command.PublicDescription,
            new PersonId(command.ResponsibleId));

        await editionRepository.SaveAsync(ct);
    }
}
