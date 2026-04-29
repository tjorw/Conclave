using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Convention.Queries;
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

    public async Task DeleteAndSaveAsync(Shift shift, CancellationToken ct = default)
    {
        db.Shifts.Remove(shift);
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
            GetEffectiveActiveStaffingCount(s))).ToList();
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
            shift.Assignments.Select(a => new StaffAssignmentDto(
                a.Id.Value,
                a.PersonId.Value,
                personNames.GetValueOrDefault(a.PersonId),
                a.Status.ToString(),
                a.AssignedAt)).ToList());
    }

    public async Task<StaffScheduleDto> GetStaffScheduleAsync(
        EditionId editionId,
        StaffAreaId? staffAreaId = null,
        CancellationToken ct = default)
    {
        var edition = await db.Editions
            .Include(e => e.ScheduleDays)
            .Include(e => e.StaffAreas)
            .Include(e => e.Stations)
            .FirstOrDefaultAsync(e => e.Id == editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        var areas = edition.StaffAreas
            .Where(a => staffAreaId is null || a.Id == staffAreaId)
            .OrderBy(a => a.Name)
            .ToList();

        var areaIds = areas.Select(a => a.Id).ToHashSet();
        var stations = edition.Stations
            .Where(s => areaIds.Contains(s.StaffAreaId))
            .OrderBy(s => s.Name)
            .ToList();

        var stationIds = stations.Select(s => s.Id).ToHashSet();
        var shifts = stationIds.Count == 0
            ? []
            : await db.Shifts
                .Include(s => s.Assignments)
                .Where(s => stationIds.Contains(s.StationId))
                .OrderBy(s => s.TimeSlot.Start)
                .ThenBy(s => s.TimeSlot.End)
                .ToListAsync(ct);

        var personIds = new HashSet<PersonId>(areas.Select(a => a.ResponsibleId));
        foreach (var shift in shifts)
            personIds.Add(shift.ResponsibleId);

        var personNames = personIds.Count == 0
            ? new Dictionary<PersonId, string>()
            : await db.Persons
                .Where(p => personIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var stationsByArea = stations
            .GroupBy(s => s.StaffAreaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var shiftsByStation = shifts
            .GroupBy(s => s.StationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new StaffScheduleDto(
            edition.Id.Value,
            staffAreaId?.Value,
            edition.ScheduleDays
                .OrderBy(d => d.Date)
                .Select(d => new EditionScheduleDayDto(d.Date, d.StartTime, d.EndTime))
                .ToList(),
            areas.Select(area => new StaffScheduleAreaDto(
                area.Id.Value,
                area.Name,
                area.Description,
                area.ResponsibleId.Value,
                personNames.GetValueOrDefault(area.ResponsibleId),
                stationsByArea.GetValueOrDefault(area.Id, [])
                    .Select(station => new StaffScheduleStationDto(
                        station.Id.Value,
                        station.Name,
                        station.Description,
                        shiftsByStation.GetValueOrDefault(station.Id, [])
                            .Select(shift =>
                            {
                                var activeAssignmentCount = GetEffectiveActiveStaffingCount(shift);
                                var confirmedAssignmentCount = shift.Assignments.Count(a =>
                                    a.Status == StaffAssignmentStatus.Confirmed);

                                return new StaffScheduleShiftDto(
                                    shift.Id.Value,
                                    shift.StationId.Value,
                                    shift.ResponsibleId.Value,
                                    personNames.GetValueOrDefault(shift.ResponsibleId),
                                    shift.TimeSlot.Start,
                                    shift.TimeSlot.End,
                                    shift.StaffingRequirement.MinPersons,
                                    shift.StaffingRequirement.MaxPersons,
                                    activeAssignmentCount,
                                    confirmedAssignmentCount,
                                    GetStaffingStatus(
                                        activeAssignmentCount,
                                        shift.StaffingRequirement.MinPersons,
                                        shift.StaffingRequirement.MaxPersons));
                            })
                            .ToList()))
                    .ToList()))
                .ToList());
    }

    private static string GetStaffingStatus(int activeAssignmentCount, int minPersons, int maxPersons)
    {
        if (activeAssignmentCount == 0)
            return "Unstaffed";
        if (activeAssignmentCount < minPersons)
            return "UnderMin";
        if (activeAssignmentCount > maxPersons)
            return "OverMax";
        if (activeAssignmentCount == maxPersons)
            return "Full";
        return "WithinRequirement";
    }

    private static int GetEffectiveActiveStaffingCount(Shift shift)
    {
        var activeAssignmentCount = shift.Assignments.Count(a =>
            a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected));
        var responsibleAlreadyAssigned = shift.Assignments.Any(a =>
            a.PersonId == shift.ResponsibleId &&
            a.Status is not (StaffAssignmentStatus.Cancelled or StaffAssignmentStatus.Rejected));

        return activeAssignmentCount + (responsibleAlreadyAssigned ? 0 : 1);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
