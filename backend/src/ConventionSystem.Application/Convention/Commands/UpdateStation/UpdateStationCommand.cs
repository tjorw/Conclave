
namespace ConventionSystem.Application.Convention.Commands.UpdateStation;

public sealed record UpdateStationCommand(
    Guid EditionId,
    Guid StationId,
    string Name,
    string? Description) : ICommand;
