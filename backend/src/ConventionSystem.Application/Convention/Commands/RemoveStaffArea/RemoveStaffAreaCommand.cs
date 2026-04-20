
namespace ConventionSystem.Application.Convention.Commands.RemoveStaffArea;

public sealed record RemoveStaffAreaCommand(Guid EditionId, Guid StaffAreaId) : ICommand;
