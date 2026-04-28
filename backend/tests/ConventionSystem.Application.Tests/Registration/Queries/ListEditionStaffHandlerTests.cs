using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries.ListEditionStaff;
using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Queries;

public class ListEditionStaffHandlerTests
{
    private readonly IStaffApplicationRepository _repo = Substitute.For<IStaffApplicationRepository>();
    private readonly ListEditionStaffHandler _handler;

    public ListEditionStaffHandlerTests()
    {
        _handler = new ListEditionStaffHandler(_repo);
    }

    [Fact]
    public async Task Handle_DelegatesTo_ListApprovedByEditionIdAsync()
    {
        var editionId = Guid.NewGuid();
        var expected = new List<EditionStaffMemberDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Bo Nilsson", "bo@example.com", null, "Confirmed"),
            new(Guid.NewGuid(), Guid.NewGuid(), "Cecilia Berg", "cecilia@example.com", "070-123456", "Assigned"),
        };
        _repo.ListApprovedByEditionIdAsync(new EditionId(editionId), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _handler.Handle(new ListEditionStaffQuery(editionId), default);

        Assert.Equal(expected, result);
        await _repo.Received(1).ListApprovedByEditionIdAsync(
            new EditionId(editionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoApprovedStaff()
    {
        var editionId = Guid.NewGuid();
        _repo.ListApprovedByEditionIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns(new List<EditionStaffMemberDto>());

        var result = await _handler.Handle(new ListEditionStaffQuery(editionId), default);

        Assert.Empty(result);
    }
}
