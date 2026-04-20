using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RemoveAvailability;

public sealed class RemoveAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : CommandHandler<RemoveAvailabilityCommand>
{
    protected override async Task ExecuteAsync(RemoveAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var availabilityId = new AvailabilityId(command.AvailabilityId);

        var context = await StaffApplicationContextLoader.LoadWithDetailsAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        context.Application.RemoveAvailability(availabilityId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
