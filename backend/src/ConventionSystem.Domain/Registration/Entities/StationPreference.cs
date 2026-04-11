using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class StationPreference
{
    public StationId StationId { get; private set; }

    private StationPreference() { }

    internal StationPreference(StationId stationId)
    {
        StationId = stationId;
    }
}
