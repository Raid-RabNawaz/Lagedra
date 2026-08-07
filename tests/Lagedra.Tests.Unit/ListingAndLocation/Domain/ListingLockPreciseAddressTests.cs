using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingLockPreciseAddressTests
{
    private static Listing NewListing() =>
        Listing.Create(
            landlordUserId: Guid.NewGuid(),
            propertyType: PropertyType.House,
            title: "Address lock test",
            description: "Needs a precise address for the lease PDF.",
            monthlyRentCents: 250_000,
            bedrooms: 3,
            bathrooms: 2m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 250_000);

    private static Address SampleAddress() =>
        new("8917 Wakefield Ave", "Panorama City", "CA", "91402", "US");

    [Fact]
    public void Allows_locking_address_after_listing_is_activated()
    {
        var listing = NewListing();
        listing.LockPreciseAddress(SampleAddress(), "US-CA");
        listing.SetApproxLocation(new GeoPoint(34.2247, -118.4498));
        listing.SubmitForReview();
        listing.ApproveByAdmin(Guid.NewGuid());
        listing.Activate();

        var corrected = new Address("100 Corrected St", "Panorama City", "CA", "91402", "US");
        listing.LockPreciseAddress(corrected, "US-CA");

        listing.PreciseAddress!.Street.Should().Be("100 Corrected St");
        listing.Status.Should().Be(ListingStatus.Activated);
    }
}
