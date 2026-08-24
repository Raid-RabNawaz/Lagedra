using System.Security.Cryptography;
using System.Text;

namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Validates the X-Twilio-Signature header on incoming webhooks: Twilio
/// signs the full callback URL plus the form fields (sorted by name,
/// name and value concatenated) with HMAC-SHA1 keyed by the account auth
/// token. https://www.twilio.com/docs/usage/security#validating-requests
/// </summary>
public static class TwilioRequestValidator
{
    public static bool IsValid(
        Uri requestUri,
        IEnumerable<KeyValuePair<string, string>> formFields,
        string signatureHeader,
        string authToken)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(formFields);

        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(authToken))
        {
            return false;
        }

        var payload = new StringBuilder(requestUri.AbsoluteUri);
        foreach (var field in formFields.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            payload.Append(field.Key).Append(field.Value);
        }

#pragma warning disable CA5350 // HMAC-SHA1 is mandated by Twilio's webhook signature scheme
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
#pragma warning restore CA5350
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString())));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signatureHeader));
    }
}
