using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Entities;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CreateConventionHandlerTests
{
    private readonly IConventionRepository _repository = Substitute.For<IConventionRepository>();
    private readonly CreateConventionHandler _handler;

    public CreateConventionHandlerTests()
    {
        _handler = new CreateConventionHandler(_repository);
    }

    private static CreateConventionCommand ValidCommand() =>
        new("My Con", "my-con", "Anna Svensson", "anna@example.com");

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _repository.SlugExistsAsync("my-con", Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(ValidCommand(), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPersonAsAdmin()
    {
        _repository.SlugExistsAsync("my-con", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(ValidCommand(), default);

        await _repository.Received(1).CreateWithAdminAsync(
            Arg.Is<Domain.Convention.Aggregates.Convention>(c =>
                c.Name == "My Con" &&
                c.Slug == "my-con" &&
                c.Administrators.Count == 1),
            Arg.Is<Person>(p =>
                p.Name == "Anna Svensson" &&
                p.Email == "anna@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_Throws()
    {
        _repository.SlugExistsAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateConventionCommand("My Con", "taken", "Anna", "anna@example.com"), default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Has Spaces")]
    [InlineData("UPPERCASE")]
    [InlineData("special!char")]
    public async Task Handle_InvalidSlugFormat_Throws(string slug)
    {
        _repository.SlugExistsAsync(slug, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new CreateConventionCommand("My Con", slug, "Anna", "anna@example.com"), default));
    }

    [Fact]
    public async Task Handle_EmptyName_Throws()
    {
        _repository.SlugExistsAsync("valid-slug", Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new CreateConventionCommand("", "valid-slug", "Anna", "anna@example.com"), default));
    }

    [Fact]
    public async Task Handle_WithProvidedConventionId_UsesProvidedId()
    {
        var providedId = Guid.CreateVersion7();
        _repository.SlugExistsAsync("my-con", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateConventionCommand("My Con", "my-con", "Anna Svensson", "anna@example.com", providedId);
        var returnedId = await _handler.Handle(command, default);

        Assert.Equal(providedId, returnedId);
        await _repository.Received(1).CreateWithAdminAsync(
            Arg.Is<Domain.Convention.Aggregates.Convention>(c => c.Id.Value == providedId),
            Arg.Any<Person>(),
            Arg.Any<CancellationToken>());
    }
}
