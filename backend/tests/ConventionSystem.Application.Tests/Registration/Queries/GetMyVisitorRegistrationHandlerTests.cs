using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class GetMyVisitorRegistrationHandlerTests
{
    private readonly IVisitorRegistrationRepository _repo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetMyVisitorRegistrationHandler _handler;

    public GetMyVisitorRegistrationHandlerTests()
    {
        _handler = new GetMyVisitorRegistrationHandler(_repo, _currentUser);
    }

    [Fact]
    public async Task Handle_DelegatesToRepositoryWithCurrentUserAndEdition()
    {
        var personId = PersonId.New();
        var editionId = Guid.NewGuid();
        var expected = new List<MyVisitorRegistrationDto>
        {
            new(Guid.NewGuid(), "Confirmed", "Helgbiljett", Guid.NewGuid(), 250, "Visitor", "Active", null, null, false),
        };

        _currentUser.PersonId.Returns(personId);
        _repo.ListByPersonAndEditionAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new GetMyVisitorRegistrationQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListByPersonAndEditionAsync(personId, new EditionId(editionId), Arg.Any<CancellationToken>());
    }
}
