using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.ReactivatePerson;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class ReactivatePersonHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ReactivatePersonHandler _handler;

    public ReactivatePersonHandlerTests()
    {
        _handler = new ReactivatePersonHandler(_conventionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person person) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.DeactivatePerson(person);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        return (convention, person);
    }

    [Fact]
    public async Task Handle_InactivePerson_ReactivatesPerson()
    {
        var (_, person) = Setup();

        await _handler.Handle(new ReactivatePersonCommand(person.Id.Value), default);

        Assert.True(person.IsActive);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, person) = Setup();

        await _handler.Handle(new ReactivatePersonCommand(person.Id.Value), default);

        await _personRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PersonNotFound_Throws()
    {
        _personRepo.GetByIdAsync(Arg.Any<PersonId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ReactivatePersonCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_AlreadyActivePerson_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var person = convention.CreatePerson("Anna", "anna@example.com");
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);

        await Assert.ThrowsAsync<PersonAlreadyActiveException>(
            () => _handler.Handle(new ReactivatePersonCommand(person.Id.Value), default));
    }
}
