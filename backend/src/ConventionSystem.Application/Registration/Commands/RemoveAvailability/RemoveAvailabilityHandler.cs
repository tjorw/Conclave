using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RemoveAvailability;

public sealed class RemoveAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : CommandHandler<RemoveAvailabilityCommand>
{
    protected override async Task ExecuteAsync(RemoveAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var availabilityId = new AvailabilityId(command.AvailabilityId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansökan", command.StaffApplicationId.ToString());

        application.RemoveAvailability(availabilityId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
