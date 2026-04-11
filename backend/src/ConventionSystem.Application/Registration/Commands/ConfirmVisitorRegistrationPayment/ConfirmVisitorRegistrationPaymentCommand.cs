using MediatR;

namespace ConventionSystem.Application.Registration.Commands.ConfirmVisitorRegistrationPayment;

public sealed record ConfirmVisitorRegistrationPaymentCommand(
    Guid VisitorRegistrationId,
    string ExternalReference) : IRequest;
