
namespace ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;

public sealed record SubmitVisitorRegistrationCommand(
    Guid EditionId,
    Guid TicketTypeId) : ICommand<Guid>;
