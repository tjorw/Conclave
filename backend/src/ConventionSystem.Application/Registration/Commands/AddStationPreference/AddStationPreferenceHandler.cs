using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddStationPreference;

public sealed class AddStationPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository)
    : IRequestHandler<AddStationPreferenceCommand>
{
    public async Task Handle(AddStationPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var stationId = new StationId(command.StationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Staffansökan '{command.StaffApplicationId}' hittades inte.");

        var edition = await editionRepository.GetByIdWithStructureAsync(application.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        if (!edition.Stations.Any(s => s.Id == stationId))
            throw new InvalidOperationException("Stationen hittades inte på denna upplaga.");

        application.AddStationPreference(stationId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
