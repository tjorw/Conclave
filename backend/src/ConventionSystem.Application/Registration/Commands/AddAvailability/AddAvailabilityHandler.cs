using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddAvailability;

public sealed class AddAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
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

        var availability = context.Application.AddAvailability(command.From, command.To);
        await staffApplicationRepository.SaveAsync(ct);
        return availability.Id.Value;
    }
}
