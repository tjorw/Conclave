using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Commands.SetCoOrganiserCount;

public sealed record SetCoOrganiserCountCommand(Guid EventId, int Count) : ICommand;
