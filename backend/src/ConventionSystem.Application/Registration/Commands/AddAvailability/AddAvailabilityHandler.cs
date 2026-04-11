using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddAvailability;

public sealed class AddAvailabilityHandler(
    IStaffApplicationRepository staffApplicationRepository)
    : IRequestHandler<AddAvailabilityCommand, Guid>
{
    public async Task<Guid> Handle(AddAvailabilityCommand command, CancellationToken ct)
    {
        var applicationId = new StaffApplicationId(command.StaffApplicationId);

        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Staffansökan '{command.StaffApplicationId}' hittades inte.");

        var availability = application.AddAvailability(command.From, command.To);
        await staffApplicationRepository.SaveAsync(ct);
        return availability.Id.Value;
    }
}
