using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class StaffAreaPreference
{
    public StaffAreaId StaffAreaId { get; private set; }

    private StaffAreaPreference() { }

    internal StaffAreaPreference(StaffAreaId staffAreaId)
    {
        StaffAreaId = staffAreaId;
    }
}
