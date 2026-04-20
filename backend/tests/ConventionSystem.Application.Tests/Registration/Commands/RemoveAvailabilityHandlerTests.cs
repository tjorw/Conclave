using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RemoveAvailability;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RemoveAvailabilityHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly RemoveAvailabilityHandler _handler;

    public RemoveAvailabilityHandlerTests()
    {
        _handler = new RemoveAvailabilityHandler(_applicationRepo);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesAvailability()
    {
        var application = new StaffApplication(StaffApplicationId.New(), PersonId.New(), EditionId.New(), "Intresserad");
        var availability = application.AddAvailability(
            new DateTime(2027, 3, 1, 10, 0, 0),
            new DateTime(2027, 3, 1, 18, 0, 0));
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);

        await _handler.Handle(new RemoveAvailabilityCommand(application.Id.Value, availability.Id.Value), default);

        Assert.Empty(application.Availabilities);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RemoveAvailabilityCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
