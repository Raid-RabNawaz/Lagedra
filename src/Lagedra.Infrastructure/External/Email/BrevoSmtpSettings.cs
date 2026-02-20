namespace Lagedra.Infrastructure.External.Email;

public sealed class BrevoSmtpSettings
{
    public const string SectionName = "Brevo";

    public string SmtpHost { get; init; } = "smtp-relay.brevo.com";
    public int SmtpPort { get; init; } = 587;
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public string FromName { get; init; } = "Lagedra";
}
