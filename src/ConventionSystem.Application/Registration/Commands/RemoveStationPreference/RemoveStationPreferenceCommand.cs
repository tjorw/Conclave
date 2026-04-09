using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RemoveStationPreference;

public sealed record RemoveStationPreferenceCommand(
    Guid StaffApplicationId,
    Guid StationId) : IRequest;
