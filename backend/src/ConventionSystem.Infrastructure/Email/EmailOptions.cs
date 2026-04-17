namespace ConventionSystem.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Logging";
    public string FromName { get; set; } = "Konvent";
    public string FromEmail { get; set; } = "noreply@example.com";

    public SmtpEmailOptions Smtp { get; set; } = new();
    public SendGridEmailOptions SendGrid { get; set; } = new();
}

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; }
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class SendGridEmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
