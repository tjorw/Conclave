
namespace ConventionSystem.Application.Registration.Commands.AddStaffAreaPreference;

public sealed record AddStaffAreaPreferenceCommand(
    Guid StaffApplicationId,
    Guid StaffAreaId) : ICommand;
