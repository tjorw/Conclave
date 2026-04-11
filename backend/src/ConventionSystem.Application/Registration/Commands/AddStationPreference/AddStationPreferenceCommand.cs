using MediatR;

namespace ConventionSystem.Application.Registration.Commands.AddStationPreference;

public sealed record AddStationPreferenceCommand(
    Guid StaffApplicationId,
    Guid StationId) : IRequest;
