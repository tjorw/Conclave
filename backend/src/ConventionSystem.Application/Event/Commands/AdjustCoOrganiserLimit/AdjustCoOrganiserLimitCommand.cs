using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.AdjustCoOrganiserLimit;

public sealed record AdjustCoOrganiserLimitCommand(Guid EventId, int Limit) : ICommand;
