using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RemoveAvailability;

public sealed class RemoveAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : IRequestHandler<RemoveAvailabilityCommand>
{
    public async Task Handle(RemoveAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);
        var availabilityId = new AvailabilityId(command.AvailabilityId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Staffansökan '{command.StaffApplicationId}' hittades inte.");

        application.RemoveAvailability(availabilityId);
        await staffApplicationRepository.SaveAsync(ct);
    }
}
