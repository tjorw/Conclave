using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Application.Convention.Queries.SearchPersonsForReception;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Queries;

public class SearchPersonsForReceptionHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly SearchPersonsForReceptionHandler _handler;

    public SearchPersonsForReceptionHandlerTests()
    {
        _handler = new SearchPersonsForReceptionHandler(
            _editionRepo, _conventionRepo, _currentUser, _personRepo,
            Substitute.For<Application.Registration.Abstractions.ITicketRepository>());
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
    public async Task Handle_TextSearch_CallsSearchForReceptionAsync()
    {
        var (convention, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var expected = new List<PersonSearchResultDto>
        {
            new(Guid.NewGuid(), "Anna Larsson", "anna@example.com", null, [])
        };
        _personRepo.SearchForReceptionAsync(
            convention.Id, edition.Id, "anna", 20, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(
            new SearchPersonsForReceptionQuery(edition.Id.Value, "anna"), default);

        Assert.Equal(expected, result);
        await _personRepo.Received(1).SearchForReceptionAsync(
            convention.Id, edition.Id, "anna", 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketIdSearch_CallsFindByTicketIdForReceptionAsync()
    {
        var (_, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var ticketId = Guid.NewGuid();
        var expected = new PersonSearchResultDto(Guid.NewGuid(), "Erik Svensson", "erik@example.com", null, []);
        _personRepo.FindByTicketIdForReceptionAsync(
            edition.Id, new TicketId(ticketId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(
            new SearchPersonsForReceptionQuery(edition.Id.Value, ticketId.ToString()), default);

        Assert.Single(result);
        Assert.Equal(expected, result[0]);
    }

    [Fact]
    public async Task Handle_TicketIdNotFound_ReturnsEmptyList()
    {
        var (_, edition, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var ticketId = Guid.NewGuid();
        _personRepo.FindByTicketIdForReceptionAsync(
            edition.Id, Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((PersonSearchResultDto?)null);

        var result = await _handler.Handle(
            new SearchPersonsForReceptionQuery(edition.Id.Value, ticketId.ToString()), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NonReceptionNonAdmin_ThrowsForbiddenException()
    {
        var (convention, edition, _, _) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new SearchPersonsForReceptionQuery(edition.Id.Value, "test"), default));
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdWithReceptionStaffAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new SearchPersonsForReceptionQuery(Guid.NewGuid(), "test"), default));
    }

    [Fact]
    public async Task Handle_ReceptionStaffMember_HasAccess()
    {
        var (_, edition, _, receptionist) = Setup();
        _currentUser.PersonId.Returns(receptionist.Id);
        _personRepo.SearchForReceptionAsync(
            Arg.Any<ConventionId>(), Arg.Any<EditionId>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonSearchResultDto>());

        var result = await _handler.Handle(
            new SearchPersonsForReceptionQuery(edition.Id.Value, "test"), default);

        Assert.Empty(result);
    }
}
