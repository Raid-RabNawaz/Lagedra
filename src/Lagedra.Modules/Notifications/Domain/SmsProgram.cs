namespace Lagedra.Modules.Notifications.Domain;

/// <summary>
/// Copy used for A2P 10DLC registration, HELP auto-replies, and legal pages.
/// Keep in lockstep with <c>apps/web/src/features/legal/smsProgram.ts</c>.
/// </summary>
public static class SmsProgram
{
    public const string Frequency = "up to 8 messages per month";

    public const string HelpReply =
        "Lagedra alerts: booking and payment activity, important account updates, and occasional offers. "
        + "Msg & data rates may apply. Reply HELP for help or STOP to cancel. "
        + "info@lagedra.com or 213-735-2362";

    public const string StopReply =
        "You are unsubscribed from Lagedra automated texts. You will not receive campaign messages. "
        + "Reply START to opt in again.";

    public const string StartReply =
        "Lagedra: You are now opted-in to automated texts about bookings, payments, account updates, "
        + "and occasional offers. Msg & data rates may apply. For help, reply HELP. To opt-out, reply STOP. "
        + "www.lagedra.com/sms";

    public static bool IsStopKeyword(string? body) =>
        Matches(body, "STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT");

    public static bool IsStartKeyword(string? body) =>
        Matches(body, "START", "UNSTOP", "YES");

    public static bool IsHelpKeyword(string? body) =>
        Matches(body, "HELP", "INFO");

    private static bool Matches(string? body, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        var token = body.Trim();
        return keywords.Any(k => token.Equals(k, StringComparison.OrdinalIgnoreCase));
    }
}
