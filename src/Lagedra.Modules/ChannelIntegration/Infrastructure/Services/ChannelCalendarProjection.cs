using Lagedra.Infrastructure.External.Channels;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Maps a PMS calendar onto Lagedra host-blocked ranges. Channel date blocks
/// are inclusive on both ends (night-based); Lagedra check-out is exclusive.
/// </summary>
public static class ChannelCalendarProjection
{
    public static IReadOnlyList<ListingDateRangeDto> ToHostBlockedRanges(
        IEnumerable<ChannelDateBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        return blocks
            .Where(b => !b.Available && b.End >= b.Start)
            .Select(b => new ListingDateRangeDto(b.Start, b.End.AddDays(1)))
            .Where(r => r.CheckOut > r.CheckIn)
            .ToList();
    }
}
