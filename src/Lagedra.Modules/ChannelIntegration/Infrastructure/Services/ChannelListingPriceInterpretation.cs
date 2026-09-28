using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Turns a PMS listing snapshot into monthly rent, using the host's saved
/// monthly-or-nightly choice when they have made one.
/// </summary>
public static class ChannelListingPriceInterpretation
{
    /// <summary>
    /// The raw feed amount, before a ×30 conversion. A monthly reading stores
    /// that amount on <see cref="ChannelListingSnapshot.MonthlyRentCents"/> and
    /// leaves nightly empty.
    /// </summary>
    public static long? SourceAmountCents(ChannelListingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.PriceBasis == ChannelPriceBasis.Monthly)
        {
            return snapshot.MonthlyRentCents is > 0 ? snapshot.MonthlyRentCents : null;
        }

        if (snapshot.NightlyRateCents is > 0)
        {
            return snapshot.NightlyRateCents;
        }

        return snapshot.MonthlyRentCents is > 0 ? snapshot.MonthlyRentCents : null;
    }

    /// <summary>
    /// Applies <paramref name="savedBasis"/> when the host has chosen.
    /// Otherwise keeps the provider's own reading (explicit nightly, or the
    /// unlabeled cap).
    /// </summary>
    public static ChannelNormalizedRate Resolve(
        ChannelListingSnapshot snapshot,
        ChannelPmsPriceBasis? savedBasis)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var source = SourceAmountCents(snapshot);
        if (savedBasis is not null && source is > 0)
        {
            var explicitBasis = savedBasis == ChannelPmsPriceBasis.Monthly
                ? ChannelPriceBasis.Monthly
                : ChannelPriceBasis.Nightly;
            return ChannelRateNormalizer.Normalize(source, explicitBasis);
        }

        if (snapshot.MonthlyRentCents is > 0)
        {
            return new ChannelNormalizedRate(
                snapshot.MonthlyRentCents,
                snapshot.NightlyRateCents,
                snapshot.PriceBasis);
        }

        return ChannelRateNormalizer.Normalize(snapshot.NightlyRateCents, ChannelPriceBasis.Nightly);
    }
}
