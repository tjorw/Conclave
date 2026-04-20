using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.CreateStation;

public sealed class CreateStationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateStationCommand, Guid>
{
    public async Task<Guid> Handle(CreateStationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;
        var staffAreaId = new StaffAreaId(command.StaffAreaId);

        var edition = await editionRepository.GetByIdWithStructureAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsible(staffAreaId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att skapa stationer för detta funktionsområde.");

        var station = edition.CreateStation(command.Name, staffAreaId, command.Description);
        await editionRepository.SaveAsync(ct);

        return station.Id.Value;
    }
}
