using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ChannelIntegration.Domain.Entities;

/// <summary>
/// Reconciliation row linking an external provider listing id to its imported
/// Lagedra listing. Lets the public redirect endpoint and the booking publisher
/// translate between the two id spaces without leaking provider ids elsewhere.
/// </summary>
public sealed class ChannelListingMap : Entity<Guid>
{
    public Guid ConnectionId { get; private set; }

    public string ProviderListingId { get; private set; } = string.Empty;

    /// <summary>Set once the external listing has been imported into Lagedra.</summary>
    public Guid? ListingId { get; private set; }

    public string? Title { get; private set; }

    public DateTime? LastImportedAt { get; private set; }

    /// <summary>
    /// When true, content sync overwrites this listing's monthly rent from the
    /// PMS on every run (including published listings). Off by default so a
    /// host's Lagedra price is not clobbered after the first import.
    /// </summary>
    public bool SyncPriceFromPms { get; private set; }

    /// <summary>
    /// When true, the availability job replaces this listing's host-blocked
    /// dates from the PMS calendar. Off by default; Lagedra booked stays are
    /// never removed by that job.
    /// </summary>
    public bool SyncCalendarFromPms { get; private set; }

    /// <summary>
    /// Host's choice for the PMS amount: nightly (× 30) or already monthly.
    /// Null until they answer after a sync.
    /// </summary>
    public ChannelPmsPriceBasis? PmsPriceBasis { get; private set; }

    /// <summary>
    /// Last raw amount from the PMS feed, in cents, before monthly conversion.
    /// Kept so a later choice can reprice the listing without another pull.
    /// </summary>
    public long? PmsSourceAmountCents { get; private set; }

    private ChannelListingMap() { }

    public static ChannelListingMap Create(
        Guid connectionId,
        string providerListingId,
        string? title,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerListingId);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new ChannelListingMap
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            ProviderListingId = providerListingId.Trim(),
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void LinkLagedraListing(Guid listingId, string? title, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        ListingId = listingId;
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        LastImportedAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public void SetSyncPreferences(bool syncPriceFromPms, bool syncCalendarFromPms, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        SyncPriceFromPms = syncPriceFromPms;
        SyncCalendarFromPms = syncCalendarFromPms;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>Stores the latest raw PMS amount. Does not change <see cref="PmsPriceBasis"/>.</summary>
    public void RememberPmsSourceAmount(long? sourceAmountCents, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (sourceAmountCents is > 0)
        {
            PmsSourceAmountCents = sourceAmountCents;
        }

        UpdatedAt = clock.UtcNow;
    }

    /// <summary>Remembers whether the PMS amount is monthly or nightly.</summary>
    public void SetPmsPriceBasis(ChannelPmsPriceBasis basis, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        PmsPriceBasis = basis;
        UpdatedAt = clock.UtcNow;
    }
}
