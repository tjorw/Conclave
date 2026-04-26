using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.ListVisitorTicketTypesForWalkup;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class ListVisitorTicketTypesForWalkupHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly ListVisitorTicketTypesForWalkupHandler _handler;

    public ListVisitorTicketTypesForWalkupHandlerTests()
    {
        _handler = new ListVisitorTicketTypesForWalkupHandler(
            _editionRepo, _conventionRepo, _currentUser, _ticketTypeRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Person admin) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evtCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staffCoord.Id, evtCoord.Id);
        var receptionist = convention.CreatePerson("Receptionist", "reception@example.com");
        edition.AddReceptionStaff(receptionist.Id, admin.Id);

        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, edition, admin);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyVisitorTypes()
    {
        var (_, edition, admin) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var allTypes = new List<TicketTypeAdminDto>
        {
            new(Guid.NewGuid(), "Dagsbiljett", 150, "Visitor", null, null, null),
            new(Guid.NewGuid(), "Funktionär", 0, "Staff", null, null, null),
            new(Guid.NewGuid(), "Arrangör", 0, "Organiser", null, null, null),
        };
        _ticketTypeRepo.ListByEditionIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(allTypes);

        var result = await _handler.Handle(
            new ListVisitorTicketTypesForWalkupQuery(edition.Id.Value), default);

        Assert.Single(result);
        Assert.Equal("Dagsbiljett", result[0].Name);
    }

    [Fact]
    public async Task Handle_NonReceptionNonAdmin_ThrowsForbiddenException()
    {
        var (convention, edition, _) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new ListVisitorTicketTypesForWalkupQuery(edition.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EmptyEdition_ReturnsEmptyList()
    {
        var (_, edition, admin) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _ticketTypeRepo.ListByEditionIdAsync(edition.Id, Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>());

        var result = await _handler.Handle(
            new ListVisitorTicketTypesForWalkupQuery(edition.Id.Value), default);

        Assert.Empty(result);
    }
}
