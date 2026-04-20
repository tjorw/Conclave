using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.DomainEventHandlers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Events;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.DomainEventHandlers;

public class ShiftCancelledHandlerTests
{
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly ShiftCancelledHandler _handler;

    public ShiftCancelledHandlerTests()
    {
        _handler = new ShiftCancelledHandler(_shiftRepo);
    }

    private static (Shift shift, PersonId assignedPerson) CreateShiftWithAssignment()
    {
        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0));
        var shift = new Shift(ShiftId.New(), StationId.New(), PersonId.New(), timeSlot, new StaffingRequirement(1, 3));
        var person = PersonId.New();
        shift.AssignPerson(person, PersonId.New());
        return (shift, person);
    }

    [Fact]
    public async Task Handle_ShiftWithActiveAssignments_CancelsAll()
    {
        var (shift, _) = CreateShiftWithAssignment();
        var performedById = PersonId.New();
        _shiftRepo.GetByIdWithAssignmentsAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        await _handler.Handle(new ShiftCancelled(shift.Id, shift.StationId, performedById, DateTimeOffset.UtcNow), default);

        Assert.All(shift.Assignments, a => Assert.Equal(StaffAssignmentStatus.Cancelled, a.Status));
    }

    [Fact]
    public async Task Handle_ShiftWithActiveAssignments_CallsSave()
    {
        var (shift, _) = CreateShiftWithAssignment();
        _shiftRepo.GetByIdWithAssignmentsAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        await _handler.Handle(new ShiftCancelled(shift.Id, shift.StationId, PersonId.New(), DateTimeOffset.UtcNow), default);

        await _shiftRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftWithNoActiveAssignments_DoesNotCallSave()
    {
        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0));
        var shift = new Shift(ShiftId.New(), StationId.New(), PersonId.New(), timeSlot, new StaffingRequirement(1, 3));
        _shiftRepo.GetByIdWithAssignmentsAsync(shift.Id, Arg.Any<CancellationToken>()).Returns(shift);

        await _handler.Handle(new ShiftCancelled(shift.Id, shift.StationId, PersonId.New(), DateTimeOffset.UtcNow), default);

        await _shiftRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftNotFound_DoesNotThrow()
    {
        _shiftRepo.GetByIdWithAssignmentsAsync(Arg.Any<ShiftId>(), Arg.Any<CancellationToken>())
            .Returns((Shift?)null);

        await _handler.Handle(new ShiftCancelled(ShiftId.New(), StationId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _shiftRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
