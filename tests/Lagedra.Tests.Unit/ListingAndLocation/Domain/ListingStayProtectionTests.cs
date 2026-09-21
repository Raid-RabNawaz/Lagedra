using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingStayProtectionTests
{
    private static Listing NewListing() =>
        Listing.Create(
            landlordUserId: Guid.NewGuid(),
            propertyType: PropertyType.Apartment,
            title: "Test listing",
            description: "A nice place to stay for a while.",
            monthlyRentCents: 300_000,
            bedrooms: 2,
            bathrooms: 1.5m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 500_000);

    [Fact]
    public void Defaults_to_included()
    {
        NewListing().StayProtectionEnabled.Should().BeTrue();
    }

    [Fact]
    public void Host_can_opt_out_and_back_in()
    {
        var listing = NewListing();

        listing.SetStayProtection(false);
        listing.StayProtectionEnabled.Should().BeFalse();

        listing.SetStayProtection(true);
        listing.StayProtectionEnabled.Should().BeTrue();
    }

    [Fact]
    public void Cannot_change_while_in_review()
    {
        var listing = NewListing();
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));
        listing.LockPreciseAddress(new Address("123 Main St", "San Francisco", "CA", "94102", "US"), "US-CA");
        listing.SubmitForReview();

        var act = () => listing.SetStayProtection(false);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot be edited*");
    }
}
