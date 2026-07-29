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
    /// account id for Hostaway, a client id for Guesty, the account email for
    /// OwnerRez.
    /// </summary>
    public string ExternalAccountId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Username { get; private set; }

    /// <summary>API secret/token, encrypted at rest via IEncryptionService.</summary>
    public string? EncryptedSecret { get; private set; }

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

        if (Status == ChannelConnectionStatus.Disabled)
        {
            return;
        }

        Status = ChannelConnectionStatus.Disabled;
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
