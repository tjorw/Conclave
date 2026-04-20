
namespace ConventionSystem.Application.Registration.Commands.ConfirmTicketPaymentWebhook;

public sealed record ConfirmTicketPaymentWebhookCommand(
    Guid VisitorRegistrationId,
    string ExternalReference,
    string PaymentStatus) : ICommand;
