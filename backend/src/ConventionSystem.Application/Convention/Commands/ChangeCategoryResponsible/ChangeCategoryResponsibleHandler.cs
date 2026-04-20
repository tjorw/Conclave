using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;

public sealed class ChangeCategoryResponsibleHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<ChangeCategoryResponsibleCommand>
{
    protected override async Task ExecuteAsync(ChangeCategoryResponsibleCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var categoryId = new CategoryId(command.CategoryId);
        var performedById = currentUser.PersonId;
        var newResponsibleId = new PersonId(command.NewResponsibleId);

        var context = await EditionContextLoader.LoadWithCategoriesForConventionCommandAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utföraren är inte administratör för denna konvention.");

        var newResponsible = await personRepository.GetByIdAsync(newResponsibleId, ct)
            ?? throw new InvalidOperationException($"Ansvarig person '{command.NewResponsibleId}' hittades inte.");
        if (newResponsible.ConventionId != context.Edition.ConventionId)
            throw new InvalidOperationException("Ny ansvarig person tillhör inte denna konvention.");

        context.Edition.ChangeCategoryResponsible(categoryId, newResponsibleId);
        await editionRepository.SaveAsync(ct);
    }
}
