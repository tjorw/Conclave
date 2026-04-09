using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.PublishEdition;

public sealed class PublishEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<PublishEditionCommand>
{
    public async Task Handle(PublishEditionCommand command, CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdAsync(new EditionId(command.EditionId), ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        var performedById = new PersonId(command.PerformedById);
        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        edition.Publish(performedById);
        await editionRepository.SaveAsync(ct);
    }
}
