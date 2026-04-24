
namespace ConventionSystem.Application.Registration.Commands.RemoveStaffAreaPreference;

public sealed record RemoveStaffAreaPreferenceCommand(
    Guid StaffApplicationId,
    Guid StaffAreaId) : ICommand;
