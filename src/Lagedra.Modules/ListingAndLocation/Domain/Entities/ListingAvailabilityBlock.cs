using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class ListingAvailabilityBlock : Entity<Guid>
{
    public Guid ListingId { get; private set; }
    public Guid? DealId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public AvailabilityBlockType BlockType { get; private set; }

    private ListingAvailabilityBlock() { }

    public static ListingAvailabilityBlock CreateBooked(
        Guid listingId, Guid dealId, DateOnly checkIn, DateOnly checkOut)
    {
        ValidateDates(checkIn, checkOut);

        return new ListingAvailabilityBlock
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            DealId = dealId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            BlockType = AvailabilityBlockType.Booked
        };
    }

    public static ListingAvailabilityBlock CreateHostBlocked(
        Guid listingId, DateOnly checkIn, DateOnly checkOut)
    {
        ValidateDates(checkIn, checkOut);

        return new ListingAvailabilityBlock
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            DealId = null,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            BlockType = AvailabilityBlockType.HostBlocked
        };
    }

    private static void ValidateDates(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
        {
            throw new ArgumentException("Check-out date must be after check-in date.");
        }
    }
}
