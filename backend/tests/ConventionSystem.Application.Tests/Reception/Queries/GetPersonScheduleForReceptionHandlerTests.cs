using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Reception.Abstractions;
using ConventionSystem.Application.Reception.Queries;
using ConventionSystem.Application.Reception.Queries.GetPersonScheduleForReception;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;
using ConventionEntity = ConventionSystem.Domain.Convention.Aggregates.Convention;

namespace ConventionSystem.Application.Tests.Reception.Queries;

public class GetPersonScheduleForReceptionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IReceptionScheduleRepository _scheduleRepo = Substitute.For<IReceptionScheduleRepository>();
    private readonly GetPersonScheduleForReceptionHandler _handler;

    public GetPersonScheduleForReceptionHandlerTests()
    {
        _handler = new GetPersonScheduleForReceptionHandler(
            _editionRepo, _conventionRepo, _currentUser, _scheduleRepo);
    }

    [Fact]
    public async Task Handle_ReturnsScheduleWithDailySummaryAndTotals()
    {
        var (convention, edition, receptionStaffId) = Setup();
        var targetPersonId = PersonId.New();

        var now = new DateTime(2026, 7, 4, 10, 0, 0);
        var shifts = new List<PersonShiftItemDto>
        {
            new(Guid.NewGuid(), "Reception", "Entré", new DateOnly(2026, 7, 4),
                now, now.AddHours(4), "Confirmed"),
        };
        var sessions = new List<PersonSessionItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Rollspelseventet", "Huvudarrangör", "Sal A",
                new DateOnly(2026, 7, 4), now.AddHours(5), now.AddHours(7)),
        };

        _currentUser.PersonId.Returns(receptionStaffId);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _scheduleRepo.ListShiftsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns(shifts);
        _scheduleRepo.ListOrganiserSessionsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns(sessions);

        var result = await _handler.Handle(
            new GetPersonScheduleForReceptionQuery(targetPersonId.Value, edition.Id.Value), default);

        Assert.Single(result.Shifts);
        Assert.Single(result.Sessions);
        Assert.Single(result.DailySummary);
        var day = result.DailySummary[0];
        Assert.Equal(new DateOnly(2026, 7, 4), day.Date);
        Assert.Equal(1, day.ShiftCount);
        Assert.Equal(4.0, day.ShiftHours);
        Assert.Equal(1, day.SessionCount);
        Assert.Equal(2.0, day.SessionHours);
        Assert.Equal(6.0, day.TotalHours);
        Assert.Equal(4.0, result.Total.TotalShiftHours);
        Assert.Equal(2.0, result.Total.TotalSessionHours);
        Assert.Equal(6.0, result.Total.TotalHours);
        Assert.Equal([new DateOnly(2026, 7, 4)], result.Total.WorkDays);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyScheduleWhenNoAssignments()
    {
        var (convention, edition, receptionStaffId) = Setup();
        var targetPersonId = PersonId.New();

        _currentUser.PersonId.Returns(receptionStaffId);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _scheduleRepo.ListShiftsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns([]);
        _scheduleRepo.ListOrganiserSessionsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(
            new GetPersonScheduleForReceptionQuery(targetPersonId.Value, edition.Id.Value), default);

        Assert.Empty(result.Shifts);
        Assert.Empty(result.Sessions);
        Assert.Empty(result.DailySummary);
        Assert.Equal(0.0, result.Total.TotalHours);
    }

    [Fact]
    public async Task Handle_AggregatesAcrossMultipleDays()
    {
        var (convention, edition, receptionStaffId) = Setup();
        var targetPersonId = PersonId.New();

        var day1 = new DateTime(2026, 7, 4, 10, 0, 0);
        var day2 = new DateTime(2026, 7, 5, 12, 0, 0);
        var shifts = new List<PersonShiftItemDto>
        {
            new(Guid.NewGuid(), "Info", "Infodisk", new DateOnly(2026, 7, 4),
                day1, day1.AddHours(3), "Confirmed"),
            new(Guid.NewGuid(), "Info", "Infodisk", new DateOnly(2026, 7, 5),
                day2, day2.AddHours(4), "Assigned"),
        };

        _currentUser.PersonId.Returns(receptionStaffId);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _scheduleRepo.ListShiftsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns(shifts);
        _scheduleRepo.ListOrganiserSessionsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(
            new GetPersonScheduleForReceptionQuery(targetPersonId.Value, edition.Id.Value), default);

        Assert.Equal(2, result.DailySummary.Count);
        Assert.Equal(7.0, result.Total.TotalShiftHours);
        Assert.Equal(2, result.Total.WorkDays.Count);
    }

    [Fact]
    public async Task Handle_ForbidsUnauthorizedUser()
    {
        var (convention, edition, _) = Setup();
        var unauthorizedId = PersonId.New();

        _currentUser.PersonId.Returns(unauthorizedId);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(
                new GetPersonScheduleForReceptionQuery(Guid.NewGuid(), edition.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AllowsConventionAdministrator()
    {
        var (convention, edition, _) = Setup();
        var adminId = PersonId.New();
        convention.AddAdministrator(adminId, adminId);
        var targetPersonId = PersonId.New();

        _currentUser.PersonId.Returns(adminId);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _scheduleRepo.ListShiftsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns([]);
        _scheduleRepo.ListOrganiserSessionsAsync(targetPersonId, edition.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(
            new GetPersonScheduleForReceptionQuery(targetPersonId.Value, edition.Id.Value), default);

        Assert.NotNull(result);
    }

    private (ConventionEntity Convention, Edition Edition, PersonId ReceptionStaffId) Setup()
    {
        var convention = new ConventionEntity(ConventionId.New(), "Konvent", "konvent");
        var staffCoordinator = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoordinator = convention.CreatePerson("Event", "event@example.com");

        var edition = convention.CreateEdition(
            "Upplaga",
            new DatePeriod(new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 6)),
            staffCoordinator.Id,
            eventCoordinator.Id);

        var receptionPerson = convention.CreatePerson("Reception", "reception@example.com");
        edition.AddReceptionStaff(receptionPerson.Id, staffCoordinator.Id);

        return (convention, edition, receptionPerson.Id);
    }
}
