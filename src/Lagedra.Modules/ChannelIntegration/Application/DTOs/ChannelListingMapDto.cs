using Lagedra.Modules.ChannelIntegration.Domain.Entities;

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
    DateTime? LastImportedAt);

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
            map.LastImportedAt);
    }
}
