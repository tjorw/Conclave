using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AddAvailability;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AddAvailabilityHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly AddAvailabilityHandler _handler;

    public AddAvailabilityHandlerTests()
    {
        _handler = new AddAvailabilityHandler(_applicationRepo);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAvailabilityId()
    {
        var application = new StaffApplication(StaffApplicationId.New(), PersonId.New(), EditionId.New(), "Intresserad");
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);

        var from = new DateTime(2027, 3, 1, 10, 0, 0);
        var to = new DateTime(2027, 3, 1, 18, 0, 0);

        var id = await _handler.Handle(new AddAvailabilityCommand(application.Id.Value, from, to), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsAvailabilityAndSaves()
    {
        var application = new StaffApplication(StaffApplicationId.New(), PersonId.New(), EditionId.New(), "Intresserad");
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);

        var from = new DateTime(2027, 3, 1, 10, 0, 0);
        var to = new DateTime(2027, 3, 1, 18, 0, 0);

        await _handler.Handle(new AddAvailabilityCommand(application.Id.Value, from, to), default);

        Assert.Single(application.Availabilities);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new AddAvailabilityCommand(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(8)), default));
    }
}
