using MediatR;

namespace ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;

public sealed record CancelVisitorRegistrationCommand(
    Guid VisitorRegistrationId) : IRequest;
