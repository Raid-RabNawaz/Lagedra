namespace Lagedra.Infrastructure.External.Sms;

public sealed class TwilioSettings
{
    public const string SectionName = "Twilio";

    public string AccountSid { get; init; } = string.Empty;
    public string AuthToken { get; init; } = string.Empty;

    /// <summary>
    /// Messaging Service SID (MG…). Preferred over a raw From number so
    /// sender selection and compliance stay in the Twilio console.
    /// </summary>
    public string MessagingServiceSid { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(MessagingServiceSid);
}
