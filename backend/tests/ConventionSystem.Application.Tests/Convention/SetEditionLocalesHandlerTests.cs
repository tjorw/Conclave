using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.SetEditionLocales;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention;

public class SetEditionLocalesHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetEditionLocalesHandler _handler;

    public SetEditionLocalesHandlerTests()
    {
        _handler = new SetEditionLocalesHandler(_editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_ConfiguresLocales()
    {
        var (convention, admin, edition) = SetupAdminConventionWithEdition();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new SetEditionLocalesCommand(edition.Id.Value, ["sv", "en"], "sv"),
            default);

        Assert.Equal(2, edition.Locales.Count);
        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_ThrowsResourceNotFoundException()
    {
        _editionRepo.GetByIdWithLocalesAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new SetEditionLocalesCommand(Guid.NewGuid(), ["sv"], "sv"), default));
    }

    [Fact]
    public async Task Handle_NonAdministrator_ThrowsForbiddenException()
    {
        var (convention, admin, edition) = SetupAdminConventionWithEdition();
        var nonAdmin = convention.CreatePerson("Other", "other@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new SetEditionLocalesCommand(edition.Id.Value, ["sv"], "sv"), default));
    }

    private (Domain.Convention.Aggregates.Convention, Domain.Convention.Entities.Person, Domain.Convention.Aggregates.Edition) SetupAdminConventionWithEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Evt", "evt@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _editionRepo.GetByIdWithLocalesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

        return (convention, admin, edition);
    }
}
