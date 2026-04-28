using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddAvailability;

public sealed class AddAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<AddAvailabilityCommand, Guid>
{
    public async Task<Guid> Handle(AddAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);

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

        var availability = context.Application.AddAvailability(command.From, command.To);
        await staffApplicationRepository.SaveAsync(ct);
        return availability.Id.Value;
    }
}
