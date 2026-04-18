using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UnpublishEdition;

public sealed class UnpublishEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UnpublishEditionCommand>
{
    public async Task Handle(UnpublishEditionCommand command, CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdAsync(new EditionId(command.EditionId), ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        var performedById = currentUser.PersonId;
        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        edition.Unpublish(performedById);
        await editionRepository.SaveAsync(ct);
    }
}
