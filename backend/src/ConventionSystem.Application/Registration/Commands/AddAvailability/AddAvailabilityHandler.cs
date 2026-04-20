using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.AddAvailability;

public sealed class AddAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : ICommandHandler<AddAvailabilityCommand, Guid>
{
    public async Task<Guid> Handle(AddAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansökan", command.StaffApplicationId.ToString());

        var availability = application.AddAvailability(command.From, command.To);
        await staffApplicationRepository.SaveAsync(ct);
        return availability.Id.Value;
    }
}
