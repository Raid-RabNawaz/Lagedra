using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.OwnerRez;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// One OwnerRez webhook delivery, reduced to what Lagedra acts on.
/// </summary>
/// <param name="Action">
/// Documented action, e.g. <c>entity_update</c> or <c>application_authorization_revoked</c>.
/// </param>
/// <param name="EntityType">Record type that changed, e.g. <c>booking</c>. Empty when absent.</param>
/// <param name="UserId">
/// The OwnerRez account the change belongs to, which is what a connection's
/// external account id holds. Absent on the test ping.
/// </param>
/// <param name="BookingUpdate">
/// Set only for booking changes we can act on: null for other entity types, for
/// blocks (which are not reservations), and when a create/update arrives without
/// its inline entity.
/// </param>
public sealed record OwnerRezWebhookEvent(
    string Action,
    string EntityType,
    string? UserId,
    ChannelBookingUpdate? BookingUpdate)
{
    public bool IsTestPing =>
        string.Equals(Action, "webhook_test", StringComparison.OrdinalIgnoreCase);

    public bool IsAuthorizationRevoked =>
        string.Equals(Action, "application_authorization_revoked", StringComparison.OrdinalIgnoreCase);

    public bool IsBooking =>
        string.Equals(EntityType, "booking", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Reads OwnerRez webhook deliveries: the Basic credentials on the request and the
/// JSON envelope described at https://www.ownerrez.com/support/articles/api-webhooks.
/// Kept separate from the command handler so the interpretation can be tested
/// without a database.
/// </summary>
public static class OwnerRezWebhookParser
{
    /// <summary>
    /// Verifies the Basic credentials OwnerRez was configured to send.
    ///
    /// Unlike Hostaway — where a host can register their own webhook URL — these
    /// deliveries only ever come from Lagedra's own OAuth app, whose Webhooks
    /// section requires a user and password. Missing config is therefore a
    /// deployment mistake, and accepting unauthenticated posts would let anyone who
    /// guessed the URL cancel a booking.
    /// </summary>
    public static bool IsAuthorized(string? authorizationHeader, OwnerRezChannelSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsWebhookAuthConfigured)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = authorizationHeader["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separator = decoded.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
            {
                return false;
            }

            return FixedEquals(decoded[..separator], settings.WebhookUsername)
                   && FixedEquals(decoded[(separator + 1)..], settings.WebhookPassword);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns null when the body is not a JSON object, which is the only case
    /// worth reporting as a bad request; every recognised-but-unhandled shape is
    /// returned as an event the caller can acknowledge.
    /// </summary>
    public static OwnerRezWebhookEvent? TryParse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var action = Str(root, "action") ?? string.Empty;
            var entityType = Str(root, "entity_type") ?? string.Empty;

            return new OwnerRezWebhookEvent(
                action,
                entityType,
                Str(root, "user_id"),
                BuildBookingUpdate(root, action, entityType));
        }
    }

    /// <summary>
    /// A deleted booking is treated as cancelled: OwnerRez only permits deletion
    /// when nothing references the reservation, and either way the stay is gone.
    /// Creates and updates are read from the inline entity through the provider's
    /// own parser so the webhook and polling paths agree on what is cancelled.
    /// </summary>
    private static ChannelBookingUpdate? BuildBookingUpdate(
        JsonElement root,
        string action,
        string entityType)
    {
        if (!string.Equals(entityType, "booking", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(action, "entity_delete", StringComparison.OrdinalIgnoreCase))
        {
            var entityId = Str(root, "entity_id");
            return string.IsNullOrWhiteSpace(entityId)
                ? null
                : new ChannelBookingUpdate(entityId, "cancelled", DateTime.UtcNow);
        }

        return root.TryGetProperty("entity", out var entity) && entity.ValueKind == JsonValueKind.Object
            ? OwnerRezChannelProvider.ParseBookingUpdate(entity)
            : null;
    }

    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Reads a value as text whether OwnerRez sent it as a JSON number or a string:
    /// <c>user_id</c> and <c>entity_id</c> are documented as integers but are
    /// compared here against text columns.
    /// </summary>
    private static string? Str(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : value.ToString(),
            _ => null,
        };
    }
}
