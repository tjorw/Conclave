using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Commands.UpdateStaffApplication;

public sealed record UpdateStaffApplicationCommand(
    Guid StaffApplicationId,
    string InterestDescription,
    IReadOnlyList<UpdateStaffApplicationAvailability> Availabilities,
    IReadOnlyList<Guid> StaffAreaIds) : ICommand;

public sealed record UpdateStaffApplicationAvailability(DateTime From, DateTime To);
