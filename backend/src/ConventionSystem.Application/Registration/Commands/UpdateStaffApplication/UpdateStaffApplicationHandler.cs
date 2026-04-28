using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.UpdateStaffApplication;

public sealed class UpdateStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateStaffApplicationCommand>
{
    protected override async Task ExecuteAsync(UpdateStaffApplicationCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var performedById = currentUser.PersonId;

        var context = await StaffApplicationContextLoader.LoadWithDetailsAsync(
            staffApplicationRepository,
            editionRepository,
            conventionRepository,
            applicationId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Endast administratörer kan uppdatera personalansökningar.");

        var edition = await editionRepository.GetByIdWithStructureAsync(context.Application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", context.Application.EditionId.Value.ToString());

        var staffAreaIds = command.StaffAreaIds
            .Distinct()
            .Select(id => new StaffAreaId(id))
            .ToList();

        if (staffAreaIds.Any(staffAreaId => edition.StaffAreas.All(s => s.Id != staffAreaId)))
            throw new DomainRuleViolationException("Ett eller flera staffområden hittades inte på denna upplaga.");

        context.Application.UpdateInterestDescription(command.InterestDescription);
        context.Application.ReplaceAvailabilities(command.Availabilities.Select(a => (a.From, a.To)));
        context.Application.ReplaceStaffAreaPreferences(staffAreaIds);

        await staffApplicationRepository.SaveAsync(ct);
    }
}
