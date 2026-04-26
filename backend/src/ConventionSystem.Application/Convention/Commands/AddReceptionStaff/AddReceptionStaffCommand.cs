
namespace ConventionSystem.Application.Convention.Commands.AddReceptionStaff;

public sealed record AddReceptionStaffCommand(
    Guid EditionId,
    Guid PersonId) : ICommand;
