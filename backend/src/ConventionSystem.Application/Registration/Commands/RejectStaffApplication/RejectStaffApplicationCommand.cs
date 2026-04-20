
namespace ConventionSystem.Application.Registration.Commands.RejectStaffApplication;

public sealed record RejectStaffApplicationCommand(
    Guid StaffApplicationId) : ICommand;
