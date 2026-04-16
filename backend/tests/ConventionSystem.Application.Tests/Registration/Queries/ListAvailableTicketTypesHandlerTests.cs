using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class ListAvailableTicketTypesHandlerTests
{
    private readonly ITicketTypeRepository _repo = Substitute.For<ITicketTypeRepository>();
    private readonly ListAvailableTicketTypesHandler _handler;

    public ListAvailableTicketTypesHandlerTests()
    {
        _handler = new ListAvailableTicketTypesHandler(_repo);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyPublicSellableVisitorTicketTypes()
    {
        var editionId = Guid.NewGuid();
        _repo.ListByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>
            {
                new(Guid.NewGuid(), "Helg", 1200, "Visitor", true, true),
                new(Guid.NewGuid(), "Staff", 0, "Staff", false, false),
                new(Guid.NewGuid(), "Ej publik", 900, "Visitor", true, false),
                new(Guid.NewGuid(), "Ej säljbar", 700, "Visitor", false, true),
            });

        var result = await _handler.Handle(new ListAvailableTicketTypesQuery(editionId), default);

        var single = Assert.Single(result);
        Assert.Equal("Helg", single.Name);
        Assert.Equal(1200, single.Price);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoVisibleVisitorTicketTypesExist()
    {
        var editionId = Guid.NewGuid();
        _repo.ListByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(new List<TicketTypeAdminDto>
            {
                new(Guid.NewGuid(), "Staff", 0, "Staff", false, false),
            });

        var result = await _handler.Handle(new ListAvailableTicketTypesQuery(editionId), default);

        Assert.Empty(result);
    }
}
