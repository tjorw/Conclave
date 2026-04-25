namespace ConventionSystem.Infrastructure.Email;

internal interface IDirectEmailSender
{
    Task SendAsync(EmailPayload payload, CancellationToken ct = default);
}
