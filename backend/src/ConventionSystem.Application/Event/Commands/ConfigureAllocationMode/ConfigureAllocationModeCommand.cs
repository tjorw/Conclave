namespace ConventionSystem.Application.Event.Commands.ConfigureAllocationMode;

public sealed record ConfigureAllocationModeCommand(
    Guid EventId,
    string AllocationMode) : ICommand;
