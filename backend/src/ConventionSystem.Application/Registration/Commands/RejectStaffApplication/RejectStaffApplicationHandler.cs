using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.RejectStaffApplication;

public sealed class RejectStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RejectStaffApplicationCommand>
{
    protected override async Task ExecuteAsync(RejectStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var performedById = currentUser.PersonId;

        var context = await StaffApplicationContextLoader.LoadAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan avslå personalansökningar.");
        context.Application.Reject(performedById);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
