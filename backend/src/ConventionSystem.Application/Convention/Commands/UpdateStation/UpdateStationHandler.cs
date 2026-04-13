using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdateStation;

public sealed class UpdateStationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateStationCommand>
{
    public async Task Handle(UpdateStationCommand command, CancellationToken ct)
    {
        var editionId  = new EditionId(command.EditionId);
        var stationId  = new StationId(command.StationId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdWithStructureAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(stationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att uppdatera denna station.");

        edition.UpdateStation(stationId, command.Name, command.Description);
        await editionRepository.SaveAsync(ct);
    }
}
