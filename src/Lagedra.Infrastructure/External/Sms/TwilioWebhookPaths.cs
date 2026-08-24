namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Webhook route shared by the sender (which embeds it in each message's
/// StatusCallback) and the endpoint that receives the callbacks. Twilio's
/// signature covers the exact URL, so both sides must agree on it.
/// </summary>
public static class TwilioWebhookPaths
{
    public const string SmsStatus = "/v1/webhooks/twilio/sms-status";
}
