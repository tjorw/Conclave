using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionAggregate = ConventionSystem.Domain.Convention.Aggregates.Convention;
using EditionAggregate = ConventionSystem.Domain.Convention.Aggregates.Edition;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionReceptionStaffTests
{
    private static (ConventionAggregate convention, EditionAggregate edition, Domain.Convention.Entities.Person admin, Domain.Convention.Entities.Person member) Setup()
    {
        var convention = new ConventionAggregate(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        var member = convention.CreatePerson("Receptionist", "reception@example.com");
        edition.ClearDomainEvents();
        return (convention, edition, admin, member);
    }

    [Fact]
    public void AddReceptionStaff_ValidInput_AddsAndRaisesEvent()
    {
        var (_, edition, admin, member) = Setup();

        edition.AddReceptionStaff(member.Id, admin.Id);

        Assert.True(edition.IsReceptionStaff(member.Id));
        var evt = edition.DomainEvents.OfType<ReceptionStaffAdded>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(edition.Id, evt.EditionId);
        Assert.Equal(member.Id, evt.PersonId);
        Assert.Equal(admin.Id, evt.AddedById);
    }

    [Fact]
    public void AddReceptionStaff_Duplicate_Throws()
    {
        var (_, edition, admin, member) = Setup();
        edition.AddReceptionStaff(member.Id, admin.Id);

        Assert.Throws<PersonAlreadyReceptionStaffException>(
            () => edition.AddReceptionStaff(member.Id, admin.Id));
    }

    [Fact]
    public void RemoveReceptionStaff_ValidInput_RemovesAndRaisesEvent()
    {
        var (_, edition, admin, member) = Setup();
        edition.AddReceptionStaff(member.Id, admin.Id);
        edition.ClearDomainEvents();

        edition.RemoveReceptionStaff(member.Id, admin.Id);

        Assert.False(edition.IsReceptionStaff(member.Id));
        var evt = edition.DomainEvents.OfType<ReceptionStaffRemoved>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(member.Id, evt.PersonId);
        Assert.Equal(admin.Id, evt.RemovedById);
    }

    [Fact]
    public void RemoveReceptionStaff_NotMember_Throws()
    {
        var (_, edition, admin, member) = Setup();

        Assert.Throws<PersonNotReceptionStaffException>(
            () => edition.RemoveReceptionStaff(member.Id, admin.Id));
    }

    [Fact]
    public void IsReceptionStaff_NotMember_ReturnsFalse()
    {
        var (_, edition, _, member) = Setup();

        Assert.False(edition.IsReceptionStaff(member.Id));
    }
}
