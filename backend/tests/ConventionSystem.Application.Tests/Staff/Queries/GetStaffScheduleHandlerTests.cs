using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Application.Staff.Queries.GetStaffSchedule;
using ConventionEntity = ConventionSystem.Domain.Convention.Aggregates.Convention;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Staff.Queries;

public class GetStaffScheduleHandlerTests
{
    private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
    private readonly IEditionRepository _editionRepository = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepository = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetStaffScheduleHandler _handler;

    public GetStaffScheduleHandlerTests()
    {
        _handler = new GetStaffScheduleHandler(
            _shiftRepository,
            _editionRepository,
            _conventionRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_AllowsEditionScopeForStaffCoordinator()
    {
        var convention = CreateConventionWithAdmin(out _);
        var edition = CreateEdition(convention.Id, out var staffCoordinatorId, out _);
        var dto = new StaffScheduleDto(edition.Id.Value, null, [], []);

        _currentUser.PersonId.Returns(staffCoordinatorId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _shiftRepository.GetStaffScheduleAsync(edition.Id, null, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value), default);

        Assert.Equal(dto, result);
        await _shiftRepository.Received(1).GetStaffScheduleAsync(edition.Id, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllowsStaffAreaResponsibleForOwnArea()
    {
        var convention = CreateConventionWithAdmin(out _);
        var edition = CreateEdition(convention.Id, out _, out var areaResponsibleId);
        var staffAreaId = edition.StaffAreas.Single().Id;
        var dto = new StaffScheduleDto(edition.Id.Value, staffAreaId.Value, [], []);

        _currentUser.PersonId.Returns(areaResponsibleId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _shiftRepository.GetStaffScheduleAsync(edition.Id, staffAreaId, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value, staffAreaId.Value), default);

        Assert.Equal(dto, result);
        await _shiftRepository.Received(1).GetStaffScheduleAsync(edition.Id, staffAreaId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllowsConventionAdminToFilterSpecificStaffArea()
    {
        var convention = CreateConventionWithAdmin(out var adminId);
        var edition = CreateEdition(convention.Id, out _, out _);
        var staffAreaId = edition.StaffAreas.Single().Id;
        var dto = new StaffScheduleDto(edition.Id.Value, staffAreaId.Value, [], []);

        _currentUser.PersonId.Returns(adminId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _shiftRepository.GetStaffScheduleAsync(edition.Id, staffAreaId, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value, staffAreaId.Value), default);

        Assert.Equal(dto, result);
        await _shiftRepository.Received(1).GetStaffScheduleAsync(edition.Id, staffAreaId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RejectsEditionScopeForStaffAreaResponsible()
    {
        var convention = CreateConventionWithAdmin(out _);
        var edition = CreateEdition(convention.Id, out _, out var areaResponsibleId);

        _currentUser.PersonId.Returns(areaResponsibleId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value), default));
    }

    [Fact]
    public async Task Handle_RejectsUnknownStaffAreaFilter()
    {
        var convention = CreateConventionWithAdmin(out var adminId);
        var edition = CreateEdition(convention.Id, out _, out _);
        var unknownStaffAreaId = Guid.NewGuid();

        _currentUser.PersonId.Returns(adminId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value, unknownStaffAreaId), default));

        Assert.Contains(unknownStaffAreaId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_RejectsStaffAreaResponsibleForOtherArea()
    {
        var convention = CreateConventionWithAdmin(out _);
        var edition = CreateEditionWithTwoAreas(convention.Id, out _, out var firstResponsibleId, out var secondStaffAreaId);

        _currentUser.PersonId.Returns(firstResponsibleId);
        _editionRepository.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepository.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new GetStaffScheduleQuery(edition.Id.Value, secondStaffAreaId.Value), default));
    }

    private static ConventionEntity CreateConventionWithAdmin(out PersonId adminId)
    {
        var convention = new ConventionEntity(ConventionId.New(), "Konvent", "konvent");
        adminId = PersonId.New();
        convention.AddAdministrator(adminId, adminId);
        return convention;
    }

    private static Edition CreateEdition(ConventionId conventionId, out PersonId staffCoordinatorId, out PersonId areaResponsibleId)
    {
        staffCoordinatorId = PersonId.New();
        var eventCoordinatorId = PersonId.New();
        areaResponsibleId = PersonId.New();

        var edition = new Edition(
            EditionId.New(),
            conventionId,
            "Edition",
            new DatePeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3)),
            staffCoordinatorId,
            eventCoordinatorId);

        edition.CreateStaffArea("Reception", areaResponsibleId, null);
        return edition;
    }

    private static Edition CreateEditionWithTwoAreas(
        ConventionId conventionId,
        out PersonId staffCoordinatorId,
        out PersonId firstAreaResponsibleId,
        out StaffAreaId secondStaffAreaId)
    {
        staffCoordinatorId = PersonId.New();
        var eventCoordinatorId = PersonId.New();
        firstAreaResponsibleId = PersonId.New();
        var secondAreaResponsibleId = PersonId.New();

        var edition = new Edition(
            EditionId.New(),
            conventionId,
            "Edition",
            new DatePeriod(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3)),
            staffCoordinatorId,
            eventCoordinatorId);

        edition.CreateStaffArea("Reception", firstAreaResponsibleId, null);
        secondStaffAreaId = edition.CreateStaffArea("Info", secondAreaResponsibleId, null).Id;
        return edition;
    }
}
