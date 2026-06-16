using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ChannelIntegration.Domain.Entities;

/// <summary>
/// Links a Lagedra deal to the booking record created on the external channel.
/// One row per deal pushed to a channel; doubles as the idempotency key so a
/// deal is never pushed twice.
/// </summary>
public sealed class ChannelBookingLink : Entity<Guid>
{
    public Guid ConnectionId { get; private set; }

    public Guid DealId { get; private set; }

    public string? ProviderBookingId { get; private set; }

    public ChannelBookingSyncStatus SyncStatus { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? PushedAt { get; private set; }

    private ChannelBookingLink() { }

    public static ChannelBookingLink CreatePending(Guid connectionId, Guid dealId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new ChannelBookingLink
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            DealId = dealId,
            SyncStatus = ChannelBookingSyncStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkPushed(string providerBookingId, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerBookingId);
        ArgumentNullException.ThrowIfNull(clock);

        ProviderBookingId = providerBookingId;
        SyncStatus = ChannelBookingSyncStatus.Pushed;
        LastError = null;
        PushedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public void MarkFailed(string error, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentNullException.ThrowIfNull(clock);

        SyncStatus = ChannelBookingSyncStatus.Failed;
        LastError = error;
        UpdatedAt = clock.UtcNow;
    }

    public void MarkCancelledRemotely(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        SyncStatus = ChannelBookingSyncStatus.CancelledRemotely;
        UpdatedAt = clock.UtcNow;
    }
}
