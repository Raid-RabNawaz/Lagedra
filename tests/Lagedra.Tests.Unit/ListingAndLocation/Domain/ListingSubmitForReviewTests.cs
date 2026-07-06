using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingSubmitForReviewTests
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

    private static Address SampleAddress() =>
        new("123 Main St", "San Francisco", "CA", "94102", "US");

    [Fact]
    public void Requires_approximate_location()
    {
        var listing = NewListing();

        var act = listing.SubmitForReview;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*location*");
    }

    [Fact]
    public void Requires_precise_address_so_agreement_never_seals_blank_city()
    {
        var listing = NewListing();
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));

        // Approx location alone is not enough: the binding Truth Surface seals
        // the city from the precise address, so it must be present first.
        var act = listing.SubmitForReview;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*address*");
    }

    [Fact]
    public void Succeeds_once_precise_address_is_locked()
    {
        var listing = NewListing();
        listing.LockPreciseAddress(SampleAddress(), "US-CA");
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));

        listing.SubmitForReview();

        listing.Status.Should().Be(ListingStatus.InReview);
        listing.SubmittedForReviewAt.Should().NotBeNull();
        listing.PreciseAddress.Should().NotBeNull();
        listing.PreciseAddress!.City.Should().Be("San Francisco");
    }
}
