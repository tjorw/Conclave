using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RemoveStationPreference;

public sealed class RemoveStationPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : IRequestHandler<RemoveStationPreferenceCommand>
{
    public async Task Handle(RemoveStationPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var stationId = new StationId(command.StationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Staffansökan '{command.StaffApplicationId}' hittades inte.");

        application.RemoveStationPreference(stationId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
