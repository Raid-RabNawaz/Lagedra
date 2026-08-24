namespace Lagedra.Infrastructure.External.Channels;

/// <summary>
/// Provider-agnostic contract for an external property-management system (PMS)
/// or distribution channel that Lagedra syncs listings from and pushes
/// completed bookings to. One implementation exists per integrated platform
/// (OwnerRez, Hostaway, Guesty, Hosthub, Lodgify, …), each identified by a stable
/// <see cref="ProviderKey"/>.
///
/// Nothing in this contract is OwnerRez-specific: it models the generic channel
/// lifecycle — pull content, pull/check availability, push a paid booking
/// (merchant-of-record), and pull booking status updates. Card data is never
/// part of the push because Lagedra collects payment itself.
/// </summary>
public interface IChannelProvider
{
    /// <summary>
    /// Stable, lowercase key for this provider (e.g. "ownerrez"). Must match the
    /// value stored on <c>ChannelConnection.ProviderKey</c> so the registry can
    /// route a connection to the right implementation.
    /// </summary>
    string ProviderKey { get; }

    /// <summary>Pull the full listing catalog for the connected account.</summary>
    Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default);

    /// <summary>Pull the availability calendar for a single external listing.</summary>
    Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default);

    /// <summary>Real-time availability check for a specific stay window.</summary>
    Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Push a confirmed, already-paid booking to the channel (MOR model).
    /// </summary>
    Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Pull booking status changes that occurred at or after
    /// <paramref name="changedSinceUtc"/> (e.g. host-side cancellations).
    /// </summary>
    Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default);
}
