using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RemoveAvailability;

public sealed record RemoveAvailabilityCommand(
    Guid StaffApplicationId,
    Guid AvailabilityId) : IRequest;
