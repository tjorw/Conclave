using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RemoveStaffAreaPreference;

public sealed class RemoveStaffAreaPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : CommandHandler<RemoveStaffAreaPreferenceCommand>
{
    protected override async Task ExecuteAsync(RemoveStaffAreaPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var staffAreaId = new StaffAreaId(command.StaffAreaId);

        var context = await StaffApplicationContextLoader.LoadWithDetailsAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        context.Application.RemoveStaffAreaPreference(staffAreaId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
