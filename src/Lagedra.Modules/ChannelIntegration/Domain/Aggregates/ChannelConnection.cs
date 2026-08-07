using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ChannelIntegration.Domain.Aggregates;

/// <summary>
/// A host's connection to one external PMS / channel provider (identified by
/// <see cref="ProviderKey"/>). Holds the per-account credentials needed to sync
/// listings and push bookings. Provider-agnostic: the same aggregate backs
/// OwnerRez, Hostaway, Guesty, … — only <see cref="ProviderKey"/> differs.
/// </summary>
public sealed class ChannelConnection : AggregateRoot<Guid>
{
    public Guid HostUserId { get; private set; }

    /// <summary>Lowercase provider key, e.g. "ownerrez". Routes to the IChannelProvider.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>
    /// The host's account identifier on the external platform — a numeric
    /// account id for Hostaway, a client id for Guesty, the OwnerRez user id for
    /// OwnerRez (which is how its webhook deliveries identify the account).
    /// </summary>
    public string ExternalAccountId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Username { get; private set; }

    /// <summary>
    /// API secret/token, encrypted at rest via IEncryptionService. For OAuth
    /// connections this holds the access token.
    /// </summary>
    public string? EncryptedSecret { get; private set; }

    /// <summary>
    /// OAuth refresh token, encrypted at rest. Null for providers (or apps)
    /// whose access tokens never expire, and for credential-based connections.
    /// </summary>
    public string? EncryptedRefreshToken { get; private set; }

    /// <summary>
    /// When the access token stops working. Null means it does not expire, so
    /// there is nothing to refresh.
    /// </summary>
    public DateTime? TokenExpiresAt { get; private set; }

    public ChannelConnectionStatus Status { get; private set; }

    public DateTime? LastContentSyncAt { get; private set; }

    public DateTime? LastBookingSyncAt { get; private set; }

    public string? LastError { get; private set; }

    private ChannelConnection() { }

    public static ChannelConnection Create(
        Guid hostUserId,
        string providerKey,
        string externalAccountId,
        string displayName,
        string? username,
        string? encryptedSecret,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(clock);

        if (hostUserId == Guid.Empty)
        {
            throw new ArgumentException("A host user id is required.", nameof(hostUserId));
        }

        var now = clock.UtcNow;
        var connection = new ChannelConnection
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            ProviderKey = providerKey.Trim(),
            ExternalAccountId = externalAccountId.Trim(),
            DisplayName = displayName.Trim(),
            Username = username,
            EncryptedSecret = encryptedSecret,
            Status = ChannelConnectionStatus.PendingActivation,
            CreatedAt = now,
            UpdatedAt = now
        };

        connection.AddDomainEvent(new ChannelConnectionCreatedEvent(
            connection.Id, hostUserId, connection.ProviderKey));

        return connection;
    }

    public void Activate(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == ChannelConnectionStatus.Revoked)
        {
            throw new InvalidOperationException(
                "A revoked connection has no credentials; reconnect it instead of activating it.");
        }

        if (Status == ChannelConnectionStatus.Active)
        {
            return;
        }

        Status = ChannelConnectionStatus.Active;
        LastError = null;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new ChannelConnectionStatusChangedEvent(Id, Status.ToString()));
    }

    public void Disable(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        // A revoked connection is already off and cannot be turned back on.
        if (Status is ChannelConnectionStatus.Disabled or ChannelConnectionStatus.Revoked)
        {
            return;
        }

        Status = ChannelConnectionStatus.Disabled;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new ChannelConnectionStatusChangedEvent(Id, Status.ToString()));
    }

    /// <summary>
    /// Records a freshly issued OAuth token set, whether from the initial
    /// authorization or a later refresh. <paramref name="expiresAt"/> is null for
    /// apps configured with non-expiring tokens.
    /// </summary>
    public void StoreOAuthTokens(
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        DateTime? expiresAt,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedAccessToken);
        ArgumentNullException.ThrowIfNull(clock);

        EncryptedSecret = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        TokenExpiresAt = expiresAt;
        LastError = null;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Disconnects the account: syncing stops, booking pushes stop, and the
    /// stored credentials are destroyed so they can never be used again. The row
    /// survives (see <see cref="ChannelConnectionStatus.Revoked"/>) purely to
    /// keep the listing mappings that make a later reconnect idempotent.
    /// </summary>
    public void Revoke(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == ChannelConnectionStatus.Revoked)
        {
            return;
        }

        Status = ChannelConnectionStatus.Revoked;
        Username = null;
        EncryptedSecret = null;
        EncryptedRefreshToken = null;
        TokenExpiresAt = null;
        LastError = null;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new ChannelConnectionStatusChangedEvent(Id, Status.ToString()));
    }

    /// <summary>
    /// Re-points a previously revoked connection at an account and credentials,
    /// putting it back in the pending state as if freshly connected. Used when a
    /// host reconnects a provider they had disconnected — rotating a token or
    /// switching accounts — so the existing listing mappings are reused.
    /// </summary>
    public void Relink(
        string externalAccountId,
        string displayName,
        string? username,
        string? encryptedSecret,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != ChannelConnectionStatus.Revoked)
        {
            throw new InvalidOperationException(
                "Only a revoked connection can be relinked.");
        }

        ExternalAccountId = externalAccountId.Trim();
        DisplayName = displayName.Trim();
        Username = username;
        EncryptedSecret = encryptedSecret;
        EncryptedRefreshToken = null;
        TokenExpiresAt = null;
        Status = ChannelConnectionStatus.PendingActivation;
        LastError = null;
        LastContentSyncAt = null;
        LastBookingSyncAt = null;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new ChannelConnectionStatusChangedEvent(Id, Status.ToString()));
    }

    public void MarkError(string message, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(clock);

        Status = ChannelConnectionStatus.Error;
        LastError = message;
        UpdatedAt = clock.UtcNow;
        AddDomainEvent(new ChannelConnectionStatusChangedEvent(Id, Status.ToString()));
    }

    public void RecordContentSync(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        LastContentSyncAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public void RecordBookingSync(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        LastBookingSyncAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }
}
