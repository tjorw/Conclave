using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.SetActiveEdition;

public sealed class SetActiveEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<SetActiveEditionCommand>
{
    public async Task Handle(SetActiveEditionCommand command, CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdAsync(new EditionId(command.EditionId), ct)
            ?? throw new KeyNotFoundException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new KeyNotFoundException("Konventionen hittades inte.");

        convention.SetActiveEdition(edition.Id);
        await conventionRepository.SaveAsync(ct);
    }
}
