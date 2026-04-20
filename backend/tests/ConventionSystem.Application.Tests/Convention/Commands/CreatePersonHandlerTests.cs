using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreatePerson;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CreatePersonHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly CreatePersonHandler _handler;

    public CreatePersonHandlerTests()
    {
        _handler = new CreatePersonHandler(_conventionRepo, _personRepo);
    }

    private Domain.Convention.Aggregates.Convention MakeConvention()
    {
        var c = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        _conventionRepo.GetByIdAsync(c.Id, Arg.Any<CancellationToken>()).Returns(c);
        return c;
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        var convention = MakeConvention();
        _personRepo.EmailExistsInConventionAsync(convention.Id, "anna@example.com", Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(
            new CreatePersonCommand(convention.Id.Value, "Anna", "anna@example.com", null), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsPerson()
    {
        var convention = MakeConvention();
        _personRepo.EmailExistsInConventionAsync(convention.Id, "anna@example.com", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(
            new CreatePersonCommand(convention.Id.Value, "Anna", "anna@example.com", "070-111"), default);

        await _personRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Convention.Entities.Person>(p =>
                p.Name == "Anna" &&
                p.Email == "anna@example.com" &&
                p.Phone == "070-111" &&
                p.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConventionNotFound_Throws()
    {
        _conventionRepo.GetByIdAsync(Arg.Any<ConventionId>(), Arg.Any<CancellationToken>()).Returns((Domain.Convention.Aggregates.Convention?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreatePersonCommand(Guid.NewGuid(), "Anna", "anna@example.com", null), default));
    }

    [Fact]
    public async Task Handle_DuplicateEmail_Throws()
    {
        var convention = MakeConvention();
        _personRepo.EmailExistsInConventionAsync(convention.Id, "taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreatePersonCommand(convention.Id.Value, "Anna", "taken@example.com", null), default));
    }
}
