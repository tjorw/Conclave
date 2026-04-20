
namespace ConventionSystem.Application.Convention.Commands.RemoveVenue;

public sealed record RemoveVenueCommand(Guid EditionId, Guid VenueId) : ICommand;
