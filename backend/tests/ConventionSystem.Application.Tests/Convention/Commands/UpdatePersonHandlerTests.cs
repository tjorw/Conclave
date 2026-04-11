using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class UpdatePersonHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly UpdatePersonHandler _handler;

    public UpdatePersonHandlerTests()
    {
        _handler = new UpdatePersonHandler(_conventionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person person) Setup(
        string email = "anna@example.com")
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var person = convention.CreatePerson("Anna", email);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        return (convention, person);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesPerson()
    {
        var (_, person) = Setup();
        _personRepo.EmailExistsInConventionAsync(person.ConventionId, "new@example.com", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(new UpdatePersonCommand(person.Id.Value, "Anna S", "new@example.com", "070-999"), default);

        Assert.Equal("Anna S", person.Name);
        Assert.Equal("new@example.com", person.Email);
        Assert.Equal("070-999", person.Phone);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, person) = Setup();

        await _handler.Handle(new UpdatePersonCommand(person.Id.Value, "Anna", "anna@example.com", null), default);

        await _personRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PersonNotFound_Throws()
    {
        _personRepo.GetByIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>()).Returns((Domain.Convention.Entities.Person?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdatePersonCommand(Guid.NewGuid(), "Anna", "anna@example.com", null), default));
    }

    [Fact]
    public async Task Handle_DuplicateEmailOnChange_Throws()
    {
        var (_, person) = Setup("anna@example.com");
        _personRepo.EmailExistsInConventionAsync(person.ConventionId, "taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new UpdatePersonCommand(person.Id.Value, "Anna", "taken@example.com", null), default));
    }

    [Fact]
    public async Task Handle_SameEmailNoChange_DoesNotCheckDuplication()
    {
        var (_, person) = Setup("anna@example.com");

        await _handler.Handle(new UpdatePersonCommand(person.Id.Value, "Anna S", "anna@example.com", null), default);

        await _personRepo.DidNotReceive().EmailExistsInConventionAsync(
            Arg.Any<ConventionId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
