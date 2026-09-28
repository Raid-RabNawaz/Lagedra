using Lagedra.Modules.ListingAndLocation.Domain.Entities;

namespace Lagedra.Modules.ListingAndLocation.Domain.Services;

public static class AvailabilityService
{
    public static bool IsAvailable(
        IReadOnlyList<ListingAvailabilityBlock> existingBlocks,
        DateOnly checkIn,
        DateOnly checkOut)
    {
        ArgumentNullException.ThrowIfNull(existingBlocks);

        if (checkOut <= checkIn)
        {
            return false;
        }

        foreach (var block in existingBlocks)
        {
            if (checkIn < block.CheckOutDate && checkOut > block.CheckInDate)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when one stay covers every night of a search window. A booking
    /// that only overlaps the middle of the window does not.
    /// SearchListingsQuery repeats these comparisons so EF can translate them.
    /// </summary>
    public static bool CoversEntireWindow(
        DateOnly blockCheckIn,
        DateOnly blockCheckOut,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        if (windowEnd <= windowStart || blockCheckOut <= blockCheckIn)
        {
            return false;
        }

        return blockCheckIn <= windowStart && blockCheckOut >= windowEnd;
    }
}
