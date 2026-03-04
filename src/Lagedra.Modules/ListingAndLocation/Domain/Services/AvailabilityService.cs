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
}
