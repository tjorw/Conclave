using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddStaffAreaPreference;

public sealed class AddStaffAreaPreferenceHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<AddStaffAreaPreferenceCommand>
{
    protected override async Task ExecuteAsync(AddStaffAreaPreferenceCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var staffAreaId = new StaffAreaId(command.StaffAreaId);

        var context = await StaffApplicationContextLoader.LoadWithDetailsAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);
        ApplicationAuthorization.EnsureConventionAdminOrOwner(
            context.Convention,
            context.Application.PersonId,
            currentUser.PersonId,
            "Du har inte behörighet att uppdatera den här personalansökan.");

        var edition = await editionRepository.GetByIdWithStructureAsync(context.Application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", context.Application.EditionId.Value.ToString());

        if (!edition.StaffAreas.Any(s => s.Id == staffAreaId))
            throw new DomainRuleViolationException("Staffomradet hittades inte pa denna upplaga.");

        context.Application.AddStaffAreaPreference(staffAreaId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
