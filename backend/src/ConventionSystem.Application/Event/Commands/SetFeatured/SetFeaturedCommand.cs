namespace ConventionSystem.Application.Event.Commands.SetFeatured;

public sealed record SetFeaturedCommand(
    Guid EventId,
    bool IsFeatured,
    int? FeaturedSortOrder) : ICommand;
