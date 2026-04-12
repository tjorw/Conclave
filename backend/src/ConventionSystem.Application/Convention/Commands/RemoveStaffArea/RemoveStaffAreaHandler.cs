using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.RemoveStaffArea;

public sealed class RemoveStaffAreaHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveStaffAreaCommand>
{
    public async Task Handle(RemoveStaffAreaCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdWithStructureAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var (area, stations) = edition.RemoveStaffArea(new StaffAreaId(command.StaffAreaId));
        foreach (var station in stations) editionRepository.MarkAsRemoved(station);
        editionRepository.MarkAsRemoved(area);
        await editionRepository.SaveAsync(ct);
    }
}
