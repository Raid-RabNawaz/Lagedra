namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Webhook route shared by the sender (which embeds it in each message's
/// StatusCallback) and the endpoint that receives the callbacks. Twilio's
/// signature covers the exact URL, so both sides must agree on it.
/// </summary>
public static class TwilioWebhookPaths
{
    public const string SmsStatus = "/v1/webhooks/twilio/sms-status";

    /// <summary>
    /// Incoming-message webhook. Point the Twilio Messaging Service (or
    /// phone-number) "A message comes in" URL here so STOP / START / HELP
    /// persist A2P consent and receive the programmed replies.
    /// </summary>
    public const string SmsInbound = "/v1/webhooks/twilio/sms-inbound";
}
