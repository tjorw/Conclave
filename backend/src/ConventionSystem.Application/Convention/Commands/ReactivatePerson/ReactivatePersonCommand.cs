
namespace ConventionSystem.Application.Convention.Commands.ReactivatePerson;

public sealed record ReactivatePersonCommand(Guid PersonId) : ICommand;
