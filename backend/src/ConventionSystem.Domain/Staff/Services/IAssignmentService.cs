using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;

namespace ConventionSystem.Domain.Staff.Services;

public interface IAssignmentService
{
    /// <summary>
    /// Kontrollerar om personen har ett överlappande pass.
    /// Bara en varning – blockerar inte tilldelning.
    /// </summary>
    bool HasTimeOverlap(PersonId personId, TimeSlot timeSlot);

    /// <summary>Hämtar personens angivna tillgänglighet från Registration-kontexten.</summary>
    IReadOnlyList<TimeSlot> GetAvailability(PersonId personId);
}
