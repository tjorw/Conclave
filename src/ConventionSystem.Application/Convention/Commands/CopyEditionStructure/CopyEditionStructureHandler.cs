using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CopyEditionStructure;

public sealed class CopyEditionStructureHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<CopyEditionStructureCommand>
{
    public async Task Handle(CopyEditionStructureCommand command, CancellationToken ct)
    {
        var targetId = new EditionId(command.TargetEditionId);
        var sourceId = new EditionId(command.SourceEditionId);
        var performedById = new PersonId(command.PerformedById);

        var target = await editionRepository.GetByIdWithStructureAsync(targetId, ct)
            ?? throw new InvalidOperationException($"Målupplaga '{command.TargetEditionId}' hittades inte.");

        var source = await editionRepository.GetByIdWithStructureAsync(sourceId, ct)
            ?? throw new InvalidOperationException($"Källupplaga '{command.SourceEditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(target.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        if (source.ConventionId != target.ConventionId)
            throw new InvalidOperationException("Käll- och målupplaga tillhör inte samma konvention.");

        target.CopyStructure(source.Id, source.Venues, source.StaffAreas, source.Stations, performedById);
        await editionRepository.SaveAsync(ct);
    }
}
