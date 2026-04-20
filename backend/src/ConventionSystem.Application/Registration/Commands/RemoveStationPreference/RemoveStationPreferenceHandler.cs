using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RemoveStationPreference;

public sealed class RemoveStationPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : CommandHandler<RemoveStationPreferenceCommand>
{
    protected override async Task ExecuteAsync(RemoveStationPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var stationId = new StationId(command.StationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansökan", command.StaffApplicationId.ToString());

        application.RemoveStationPreference(stationId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
