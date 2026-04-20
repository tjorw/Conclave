
namespace ConventionSystem.Application.Convention.Commands.RemoveStation;

public sealed record RemoveStationCommand(
    Guid EditionId,
    Guid StationId) : ICommand;
