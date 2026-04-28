using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.DeleteStaffApplication;

public sealed class DeleteStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<DeleteStaffApplicationCommand>
{
    protected override async Task ExecuteAsync(DeleteStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);

        var context = await StaffApplicationContextLoader.LoadAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            currentUser.PersonId,
            "Endast administratörer kan ta bort personalansökningar.");

        await staffApplicationRepository.DeleteAsync(context.Application, ct);
    }
}
