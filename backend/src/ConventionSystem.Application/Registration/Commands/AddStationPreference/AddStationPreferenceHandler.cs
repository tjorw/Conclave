using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddStationPreference;

public sealed class AddStationPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : CommandHandler<AddStationPreferenceCommand>
{
    protected override async Task ExecuteAsync(AddStationPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var stationId = new StationId(command.StationId);

        var context = await StaffApplicationContextLoader.LoadWithDetailsAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        var edition = await editionRepository.GetByIdWithStructureAsync(context.Application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", context.Application.EditionId.Value.ToString());

        if (!edition.Stations.Any(s => s.Id == stationId))
            throw new DomainRuleViolationException("Stationen hittades inte på denna upplaga.");

        context.Application.AddStationPreference(stationId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
