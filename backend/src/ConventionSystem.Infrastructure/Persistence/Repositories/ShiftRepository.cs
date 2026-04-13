using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Enums;
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

    public async Task<IReadOnlyList<ShiftSummaryDto>> ListByStationIdAsync(StationId id, CancellationToken ct = default)
    {
        var shifts = await db.Shifts
            .Include(s => s.Assignments)
            .Where(s => s.StationId == id)
            .ToListAsync(ct);

        var responsibleIds = shifts.Select(s => s.ResponsibleId).Distinct().ToHashSet();
        var personNames = await db.Persons
            .Where(p => responsibleIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return shifts.Select(s => new ShiftSummaryDto(
            s.Id.Value,
            s.StationId.Value,
            s.ResponsibleId.Value,
            personNames.GetValueOrDefault(s.ResponsibleId),
            s.TimeSlot.Start,
            s.TimeSlot.End,
            s.StaffingRequirement.MinPersons,
            s.StaffingRequirement.MaxPersons,
            s.Assignments.Count(a => a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected)),
            s.Status.ToString())).ToList();
    }

    public async Task<ShiftDto?> GetProjectedByIdAsync(ShiftId id, CancellationToken ct = default)
    {
        var shift = await db.Shifts
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (shift is null) return null;

        var personIds = shift.Assignments.Select(a => a.PersonId)
            .Append(shift.ResponsibleId).Distinct().ToHashSet();
        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return new ShiftDto(
            shift.Id.Value,
            shift.StationId.Value,
            shift.ResponsibleId.Value,
            personNames.GetValueOrDefault(shift.ResponsibleId),
            shift.TimeSlot.Start,
            shift.TimeSlot.End,
            shift.StaffingRequirement.MinPersons,
            shift.StaffingRequirement.MaxPersons,
            shift.Status.ToString(),
            shift.Assignments.Select(a => new StaffAssignmentDto(
                a.Id.Value,
                a.PersonId.Value,
                personNames.GetValueOrDefault(a.PersonId),
                a.Status.ToString(),
                a.AssignedAt)).ToList());
    }

    public void MarkAsAdded<T>(T entity) where T : class
        => db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Added;

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
