using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class ListAvailableTicketTypesHandlerTests
{
    private readonly ITicketTypeRepository _repo = Substitute.For<ITicketTypeRepository>();
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ListAvailableTicketTypesHandler _handler;

    public ListAvailableTicketTypesHandlerTests()
    {
        _currentUser.PersonId.Returns(new PersonId(Guid.NewGuid()));
        _handler = new ListAvailableTicketTypesHandler(_repo, _registrationRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyVisitorTicketTypes()
    {
        var editionId = Guid.NewGuid();
        var helgId = Guid.NewGuid();
        _repo.ListByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>
            {
                new(helgId, "Helg", 1200, "Visitor", null, null),
                new(Guid.NewGuid(), "Staff", 0, "Staff", null, null),
            });
        _registrationRepo
            .HasActiveRegistrationForTicketTypeAsync(
                _currentUser.PersonId,
                Arg.Any<EditionId>(),
                Arg.Any<TicketTypeId>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new ListAvailableTicketTypesQuery(editionId), default);

        var single = Assert.Single(result);
        Assert.Equal("Helg", single.Name);
        Assert.Equal(1200, single.Price);
    }

    [Fact]
    public async Task Handle_ExcludesTicketTypesAlreadyPurchasedByCurrentUser()
    {
        var editionId = Guid.NewGuid();
        var fridayId = Guid.NewGuid();
        var saturdayId = Guid.NewGuid();

        _repo.ListByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>
            {
                new(fridayId, "Dag Fredag", 5000, "Visitor", null, null),
                new(saturdayId, "Dag Lördag", 5000, "Visitor", null, null),
            });

        _registrationRepo
            .HasActiveRegistrationForTicketTypeAsync(
                _currentUser.PersonId,
                Arg.Any<EditionId>(),
                Arg.Is<TicketTypeId>(id => id.Value == fridayId),
                Arg.Any<CancellationToken>())
            .Returns(true);

        _registrationRepo
            .HasActiveRegistrationForTicketTypeAsync(
                _currentUser.PersonId,
                Arg.Any<EditionId>(),
                Arg.Is<TicketTypeId>(id => id.Value == saturdayId),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new ListAvailableTicketTypesQuery(editionId), default);

        var single = Assert.Single(result);
        Assert.Equal(saturdayId, single.Id);
        Assert.Equal("Dag Lördag", single.Name);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoVisitorTicketTypesExist()
    {
        var editionId = Guid.NewGuid();
        _repo.ListByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>
            {
                new(Guid.NewGuid(), "Staff", 0, "Staff", null, null),
            });
        _registrationRepo
            .HasActiveRegistrationForTicketTypeAsync(
                _currentUser.PersonId,
                Arg.Any<EditionId>(),
                Arg.Any<TicketTypeId>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new ListAvailableTicketTypesQuery(editionId), default);

        Assert.Empty(result);
    }
}
