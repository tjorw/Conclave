using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;

public sealed class AcceptStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<AcceptStaffApplicationCommand>
{
    protected override async Task ExecuteAsync(AcceptStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var performedById = currentUser.PersonId;

        var context = await StaffApplicationContextLoader.LoadAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        ApplicationAuthorization.EnsureStaffApplicationManager(
            context.Convention,
            context.Edition,
            performedById,
            "Utföraren har inte behörighet att acceptera staffansökningar.");

        context.Application.Accept(performedById);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
