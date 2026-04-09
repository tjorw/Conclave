using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class ShiftRepository(ConventionDbContext db) : IShiftRepository
{
    public async Task AddAndSaveAsync(Shift shift, CancellationToken ct = default)
    {
        await db.Shifts.AddAsync(shift, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken ct = default)
        => db.Shifts.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Shift?> GetByIdWithAssignmentsAsync(ShiftId id, CancellationToken ct = default)
        => db.Shifts
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
