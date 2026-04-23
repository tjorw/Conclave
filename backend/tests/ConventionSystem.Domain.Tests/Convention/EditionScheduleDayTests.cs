using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionScheduleDayTests
{
    [Fact]
    public void CreateEdition_AddsDefaultScheduleDaysForPeriod()
    {
        var edition = CreateEdition();

        Assert.Equal(3, edition.ScheduleDays.Count);
        Assert.All(edition.ScheduleDays, day =>
        {
            Assert.Null(day.StartTime);
            Assert.Null(day.EndTime);
        });
    }

    [Fact]
    public void UpdateDetails_WithScheduleDays_UpdatesDailyTimes()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);

        edition.UpdateDetails("Test 2027", period, staff.Id, evt.Id, [
            new EditionScheduleDay(Guid.NewGuid(), new DateOnly(2027, 3, 1), new TimeOnly(9, 0), new TimeOnly(21, 0)),
            new EditionScheduleDay(Guid.NewGuid(), new DateOnly(2027, 3, 2), new TimeOnly(10, 0), new TimeOnly(22, 0)),
            new EditionScheduleDay(Guid.NewGuid(), new DateOnly(2027, 3, 3), new TimeOnly(11, 0), new TimeOnly(20, 0)),
        ]);

        Assert.Collection(edition.ScheduleDays,
            day =>
            {
                Assert.Equal(new DateOnly(2027, 3, 1), day.Date);
                Assert.Equal(new TimeOnly(9, 0), day.StartTime);
                Assert.Equal(new TimeOnly(21, 0), day.EndTime);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2027, 3, 2), day.Date);
                Assert.Equal(new TimeOnly(10, 0), day.StartTime);
                Assert.Equal(new TimeOnly(22, 0), day.EndTime);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2027, 3, 3), day.Date);
                Assert.Equal(new TimeOnly(11, 0), day.StartTime);
                Assert.Equal(new TimeOnly(20, 0), day.EndTime);
            });
    }

    [Fact]
    public void CreateScheduleDay_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new EditionScheduleDay(Guid.NewGuid(), new DateOnly(2027, 3, 1), new TimeOnly(21, 0), new TimeOnly(9, 0)));
    }

    private static Domain.Convention.Aggregates.Edition CreateEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        return convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
    }
}
