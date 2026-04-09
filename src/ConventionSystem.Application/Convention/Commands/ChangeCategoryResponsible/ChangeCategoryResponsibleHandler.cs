using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;

public sealed class ChangeCategoryResponsibleHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<ChangeCategoryResponsibleCommand>
{
    public async Task Handle(ChangeCategoryResponsibleCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var categoryId = new CategoryId(command.CategoryId);
        var performedById = new PersonId(command.PerformedById);
        var newResponsibleId = new PersonId(command.NewResponsibleId);

        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var newResponsible = await personRepository.GetByIdAsync(newResponsibleId, ct)
            ?? throw new InvalidOperationException($"Ansvarig person '{command.NewResponsibleId}' hittades inte.");
        if (newResponsible.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Ny ansvarig person tillhör inte denna konvention.");

        edition.ChangeCategoryResponsible(categoryId, newResponsibleId);
        await editionRepository.SaveAsync(ct);
    }
}
