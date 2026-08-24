using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingEditabilityTests
{
    private static Listing NewListing() =>
        Listing.Create(
            landlordUserId: Guid.NewGuid(),
            propertyType: PropertyType.Apartment,
            title: "Original title",
            description: "A nice place to stay for a while.",
            monthlyRentCents: 300_000,
            bedrooms: 2,
            bathrooms: 1.5m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 500_000);

    private static Address SampleAddress() =>
        new("123 Main St", "San Francisco", "CA", "94102", "US");

    private static Listing ReadyForReview()
    {
        var listing = NewListing();
        listing.LockPreciseAddress(SampleAddress(), "US-CA");
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));
        return listing;
    }

    private static void Rename(Listing listing, string title) =>
        listing.Update(
            listing.PropertyType,
            title,
            listing.Description,
            listing.MonthlyRentCents,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.StayRange!,
            listing.MaxDepositCents,
            listing.SquareFootage);

    [Fact]
    public void Published_listing_can_change_marketplace_details()
    {
        var listing = ReadyForReview();
        listing.SubmitForReview();
        listing.ApproveByAdmin(Guid.NewGuid());

        Rename(listing, "Updated downtown loft");
        listing.SetApproxLocation(new GeoPoint(34.0522, -118.2437));

        listing.Title.Should().Be("Updated downtown loft");
        listing.ApproxGeoPoint!.Latitude.Should().Be(34.0522);
        listing.Status.Should().Be(ListingStatus.Published);
    }

    [Fact]
    public void Activated_listing_can_change_marketplace_details()
    {
        var listing = ReadyForReview();
        listing.SubmitForReview();
        listing.ApproveByAdmin(Guid.NewGuid());
        listing.Activate();

        Rename(listing, "Updated after first booking");

        listing.Title.Should().Be("Updated after first booking");
        listing.Status.Should().Be(ListingStatus.Activated);
    }

    [Fact]
    public void In_review_listing_stays_frozen()
    {
        var listing = ReadyForReview();
        listing.SubmitForReview();

        var act = () => Rename(listing, "Changed during review");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InReview*");
    }

    [Fact]
    public void Closed_listing_stays_frozen()
    {
        var listing = ReadyForReview();
        listing.SubmitForReview();
        listing.ApproveByAdmin(Guid.NewGuid());
        listing.Close();

        var act = () => Rename(listing, "Changed after close");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Closed*");
    }
}
