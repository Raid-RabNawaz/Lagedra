namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Twilio credentials for SMS (Messaging API) and email (SendGrid).
/// SMS auth: Account SID + either Auth Token or API Key (SID + secret).
/// Email: SendGrid API key under the Twilio account.
/// </summary>
public sealed class TwilioSettings
{
    public const string SectionName = "Twilio";

    /// <summary>Account SID (AC…).</summary>
    public string AccountSid { get; init; } = string.Empty;

    /// <summary>Auth token. Optional when <see cref="ApiKeySid"/> and <see cref="ApiKeySecret"/> are set.</summary>
    public string AuthToken { get; init; } = string.Empty;

    /// <summary>API Key SID (SK…). Preferred over the account auth token.</summary>
    public string ApiKeySid { get; init; } = string.Empty;

    /// <summary>API Key secret (shown once when the key is created).</summary>
    public string ApiKeySecret { get; init; } = string.Empty;

    /// <summary>
    /// Messaging Service SID (MG…). Preferred over a raw From number so
    /// sender selection and compliance stay in the Twilio console.
    /// </summary>
    public string MessagingServiceSid { get; init; } = string.Empty;

    /// <summary>SendGrid API key (SG…) for transactional email.</summary>
    public string SendGridApiKey { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = "Lagedra";

    public bool IsSmsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(MessagingServiceSid)
        && HasSmsCredentials;

    public bool IsEmailConfigured =>
        !string.IsNullOrWhiteSpace(SendGridApiKey)
        && !string.IsNullOrWhiteSpace(FromEmail);

    private bool HasSmsCredentials =>
        (!string.IsNullOrWhiteSpace(ApiKeySid) && !string.IsNullOrWhiteSpace(ApiKeySecret))
        || !string.IsNullOrWhiteSpace(AuthToken);

    /// <summary>Basic-auth username for the Messaging API.</summary>
    public string SmsAuthUsername =>
        !string.IsNullOrWhiteSpace(ApiKeySid) ? ApiKeySid : AccountSid;

    /// <summary>Basic-auth password for the Messaging API.</summary>
    public string SmsAuthPassword =>
        !string.IsNullOrWhiteSpace(ApiKeySecret) ? ApiKeySecret : AuthToken;
}
