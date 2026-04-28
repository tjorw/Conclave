using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Commands.DeleteStaffApplication;

public sealed record DeleteStaffApplicationCommand(Guid StaffApplicationId) : ICommand;
