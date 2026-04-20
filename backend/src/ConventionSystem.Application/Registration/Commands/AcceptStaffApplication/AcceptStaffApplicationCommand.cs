
namespace ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;

public sealed record AcceptStaffApplicationCommand(
    Guid StaffApplicationId) : ICommand;
