
namespace ConventionSystem.Application.Convention.Commands.UpdateVenue;

public sealed record UpdateVenueCommand(
    Guid EditionId,
    Guid VenueId,
    string Name,
    string Building,
    string? Description) : ICommand;
