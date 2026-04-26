using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetPersonTicketsForReception;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetPersonTicketsForReceptionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly GetPersonTicketsForReceptionHandler _handler;

    public GetPersonTicketsForReceptionHandlerTests()
    {
        _handler = new GetPersonTicketsForReceptionHandler(
            _editionRepo, _conventionRepo, _currentUser, _ticketRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person receptionist) Setup()
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

        return (convention, edition, admin, receptionist);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsTickets()
    {
        var (_, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var personId = Guid.NewGuid();
        var expected = new List<PersonTicketForReceptionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Dagsbiljett", "Visitor", "Active",
                150, null, null, [], false, null, DateTimeOffset.UtcNow)
        };
        _ticketRepo.ListForReceptionAsync(new PersonId(personId), edition.Id, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(
            new GetPersonTicketsForReceptionQuery(personId, edition.Id.Value), default);

        Assert.Equal(expected, result);
        await _ticketRepo.Received(1).ListForReceptionAsync(
            new PersonId(personId), edition.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoTickets_ReturnsEmptyList()
    {
        var (_, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var personId = Guid.NewGuid();
        _ticketRepo.ListForReceptionAsync(Arg.Any<PersonId>(), Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonTicketForReceptionDto>());

        var result = await _handler.Handle(
            new GetPersonTicketsForReceptionQuery(personId, edition.Id.Value), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NonReceptionNonAdmin_ThrowsForbiddenException()
    {
        var (convention, edition, _, _) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);
        var personId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new GetPersonTicketsForReceptionQuery(personId, edition.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithReceptionStaffAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new GetPersonTicketsForReceptionQuery(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ReceptionStaffMember_HasAccess()
    {
        var (_, edition, _, receptionist) = Setup();
        _currentUser.PersonId.Returns(receptionist.Id);
        var personId = Guid.NewGuid();
        _ticketRepo.ListForReceptionAsync(Arg.Any<PersonId>(), Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonTicketForReceptionDto>());

        var result = await _handler.Handle(
            new GetPersonTicketsForReceptionQuery(personId, edition.Id.Value), default);

        Assert.Empty(result);
    }
}
