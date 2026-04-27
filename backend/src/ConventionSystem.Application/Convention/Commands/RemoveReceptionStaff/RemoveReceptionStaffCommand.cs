
namespace ConventionSystem.Application.Convention.Commands.RemoveReceptionStaff;

public sealed record RemoveReceptionStaffCommand(
    Guid EditionId,
    Guid PersonId) : ICommand;
