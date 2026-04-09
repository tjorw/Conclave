using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddAvailability;

public sealed record AddAvailabilityCommand(
    Guid StaffApplicationId,
    DateTime From,
    DateTime To) : IRequest<Guid>;
