using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddStationPreference;

public sealed class AddStationPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository)
    : CommandHandler<AddStationPreferenceCommand>
{
    protected override async Task ExecuteAsync(AddStationPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var stationId = new StationId(command.StationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansökan", command.StaffApplicationId.ToString());

        var edition = await editionRepository.GetByIdWithStructureAsync(application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", application.EditionId.Value.ToString());

        if (!edition.Stations.Any(s => s.Id == stationId))
            throw new DomainRuleViolationException("Stationen hittades inte på denna upplaga.");

        application.AddStationPreference(stationId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
