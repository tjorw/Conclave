using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Exceptions;
using ConventionSystem.Domain.Staff.Events;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;

namespace ConventionSystem.Domain.Tests.Staff;

public class ShiftTests
{
    private static Shift CreateShift(int minPersons = 1, int maxPersons = 3)
    {
        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 14, 0, 0));
        var requirement = new StaffingRequirement(minPersons, maxPersons);
        return new Shift(ShiftId.New(), StationId.New(), PersonId.New(), timeSlot, requirement);
    }

    [Fact]
    public void AssignPerson_ValidAssignment_ReturnsAssignment()
    {
        var shift = CreateShift();
        var personId = PersonId.New();

        var assignment = shift.AssignPerson(personId, PersonId.New());

        Assert.NotNull(assignment);
        Assert.Equal(personId, assignment.PersonId);
        Assert.Equal(StaffAssignmentStatus.Assigned, assignment.Status);
    }

    [Fact]
    public void AssignPerson_RaisesPersonAssignedToShiftEvent()
    {
        var shift = CreateShift();
        var personId = PersonId.New();

        shift.AssignPerson(personId, PersonId.New());

        var evt = shift.DomainEvents.OfType<PersonAssignedToShift>().Single();
        Assert.Equal(shift.Id, evt.ShiftId);
        Assert.Equal(personId, evt.PersonId);
    }

    [Fact]
    public void AssignPerson_AtMaxCapacity_Throws()
    {
        var shift = CreateShift(maxPersons: 1);
        shift.AssignPerson(PersonId.New(), PersonId.New());

        Assert.Throws<ShiftAlreadyFullyStaffedException>(() => shift.AssignPerson(PersonId.New(), PersonId.New()));
    }

    [Fact]
    public void AssignPerson_DuplicatePerson_Throws()
    {
        var shift = CreateShift();
        var personId = PersonId.New();
        shift.AssignPerson(personId, PersonId.New());

        Assert.Throws<PersonAlreadyAssignedToShiftException>(() => shift.AssignPerson(personId, PersonId.New()));
    }

    [Fact]
    public void AssignPerson_CancelledShift_Throws()
    {
        var shift = CreateShift();
        shift.Cancel(PersonId.New());

        Assert.Throws<ShiftCannotAssignInCurrentStateException>(() => shift.AssignPerson(PersonId.New(), PersonId.New()));
    }

    [Fact]
    public void ConfirmAssignment_TransitionsToConfirmed()
    {
        var shift = CreateShift();
        var assignment = shift.AssignPerson(PersonId.New(), PersonId.New());
        shift.ClearDomainEvents();

        shift.ConfirmAssignment(assignment.Id);

        Assert.Equal(StaffAssignmentStatus.Confirmed, assignment.Status);
        Assert.Single(shift.DomainEvents.OfType<AssignmentConfirmed>());
    }

    [Fact]
    public void RejectAssignment_TransitionsToRejected()
    {
        var shift = CreateShift();
        var assignment = shift.AssignPerson(PersonId.New(), PersonId.New());
        shift.ClearDomainEvents();

        shift.RejectAssignment(assignment.Id);

        Assert.Equal(StaffAssignmentStatus.Rejected, assignment.Status);
        Assert.Single(shift.DomainEvents.OfType<AssignmentRejected>());
    }

    [Fact]
    public void CancelAssignment_TransitionsToCancelled()
    {
        var shift = CreateShift();
        var assignment = shift.AssignPerson(PersonId.New(), PersonId.New());
        shift.ClearDomainEvents();

        shift.CancelAssignment(assignment.Id, PersonId.New());

        Assert.Equal(StaffAssignmentStatus.Cancelled, assignment.Status);
        Assert.Single(shift.DomainEvents.OfType<AssignmentCancelled>());
    }

    [Fact]
    public void CancelAssignment_RejectedAssignment_Throws()
    {
        var shift = CreateShift();
        var assignment = shift.AssignPerson(PersonId.New(), PersonId.New());
        shift.RejectAssignment(assignment.Id);

        Assert.Throws<RejectedAssignmentCannotBeCancelledException>(() => shift.CancelAssignment(assignment.Id, PersonId.New()));
    }

    [Fact]
    public void Cancel_TransitionsToCancelled()
    {
        var shift = CreateShift();

        shift.Cancel(PersonId.New());

        Assert.Equal(ShiftStatus.Cancelled, shift.Status);
        Assert.Single(shift.DomainEvents.OfType<ShiftCancelled>());
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var shift = CreateShift();
        shift.Cancel(PersonId.New());

        Assert.Throws<ShiftCanOnlyBeCancelledWhenPlannedException>(() => shift.Cancel(PersonId.New()));
    }

    [Fact]
    public void Update_PlannedShift_UpdatesDetails()
    {
        var shift = CreateShift();
        var stationId = StationId.New();
        var responsibleId = PersonId.New();
        var timeSlot = new TimeSlot(new DateTime(2027, 3, 1, 12, 0, 0), new DateTime(2027, 3, 1, 16, 0, 0));
        var requirement = new StaffingRequirement(2, 5);

        shift.Update(stationId, responsibleId, timeSlot, requirement);

        Assert.Equal(stationId, shift.StationId);
        Assert.Equal(responsibleId, shift.ResponsibleId);
        Assert.Equal(timeSlot.Start, shift.TimeSlot.Start);
        Assert.Equal(timeSlot.End, shift.TimeSlot.End);
        Assert.Equal(requirement.MinPersons, shift.StaffingRequirement.MinPersons);
        Assert.Equal(requirement.MaxPersons, shift.StaffingRequirement.MaxPersons);
    }

    [Fact]
    public void Update_CancelledShift_Throws()
    {
        var shift = CreateShift();
        shift.Cancel(PersonId.New());

        Assert.Throws<ShiftCanOnlyBeUpdatedWhenPlannedException>(() =>
            shift.Update(
                StationId.New(),
                PersonId.New(),
                new TimeSlot(new DateTime(2027, 3, 1, 12, 0, 0), new DateTime(2027, 3, 1, 16, 0, 0)),
                new StaffingRequirement(2, 4)));
    }

    [Fact]
    public void AssignPerson_AfterCancelAndRejected_AllowsReassign()
    {
        var shift = CreateShift();
        var personId = PersonId.New();
        var assignment = shift.AssignPerson(personId, PersonId.New());
        shift.CancelAssignment(assignment.Id, PersonId.New());

        var newAssignment = shift.AssignPerson(personId, PersonId.New());

        Assert.NotNull(newAssignment);
    }
}
