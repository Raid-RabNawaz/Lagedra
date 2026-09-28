using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;

namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

/// <summary>
/// A listing pulled from a channel and (once imported) linked to a Lagedra
/// listing. Surfaced to the host so they can see what arrived and jump to the
/// imported draft to finish setup.
/// </summary>
public sealed record ChannelListingMapDto(
    Guid Id,
    string ProviderListingId,
    Guid? ListingId,
    string? Title,
    DateTime? LastImportedAt,
    bool SyncPriceFromPms,
    bool SyncCalendarFromPms,
    ChannelPmsPriceBasis? PmsPriceBasis,
    long? PmsSourceAmountCents,
    /// <summary>
    /// True when the raw PMS amount is high enough that a nightly reading would
    /// exceed the monthly cap. The host is asked only in that case.
    /// </summary>
    bool PmsPriceNeedsConfirmation);

public static class ChannelListingMapMapper
{
    public static ChannelListingMapDto ToDto(ChannelListingMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return new ChannelListingMapDto(
            map.Id,
            map.ProviderListingId,
            map.ListingId,
            map.Title,
            map.LastImportedAt,
            map.SyncPriceFromPms,
            map.SyncCalendarFromPms,
            map.PmsPriceBasis,
            map.PmsSourceAmountCents,
            ChannelRateNormalizer.NeedsPricePeriodConfirmation(map.PmsSourceAmountCents));
    }
}
