using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
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

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _repository.SlugExistsAsync("my-con", Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(new CreateConventionCommand("My Con", "my-con"), default);

        Assert.NotEqual(Guid.Empty, id);
        await _repository.Received(1).AddAsync(Arg.Any<Domain.Convention.Aggregates.Convention>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_Throws()
    {
        _repository.SlugExistsAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CreateConventionCommand("My Con", "taken"), default));
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
            () => _handler.Handle(new CreateConventionCommand("My Con", slug), default));
    }

    [Fact]
    public async Task Handle_EmptyName_Throws()
    {
        _repository.SlugExistsAsync("valid-slug", Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new CreateConventionCommand("", "valid-slug"), default));
    }
}
