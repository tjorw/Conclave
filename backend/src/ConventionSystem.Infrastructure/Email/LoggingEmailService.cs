using Microsoft.Extensions.Logging;

namespace ConventionSystem.Infrastructure.Email;

internal sealed class LoggingEmailService(ILogger<LoggingEmailService> logger) : IDirectEmailSender
{
    public Task SendAsync(EmailPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] {Subject} -> {Name} <{Email}>", payload.Subject, payload.ToName, payload.To);
        return Task.CompletedTask;
    }
}
