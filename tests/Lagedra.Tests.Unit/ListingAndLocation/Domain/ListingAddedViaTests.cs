using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingAddedViaTests
{
    private static Listing NewListing(
        ListingAddedVia addedVia = ListingAddedVia.Manual,
        string? detail = null) =>
        Listing.Create(
            landlordUserId: Guid.NewGuid(),
            propertyType: PropertyType.Apartment,
            title: "Source test listing",
            description: "A nice place to stay for a while.",
            monthlyRentCents: 300_000,
            bedrooms: 2,
            bathrooms: 1.5m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 500_000,
            addedVia: addedVia,
            addedViaDetail: detail);

    [Fact]
    public void Create_defaults_to_manual()
    {
        var listing = NewListing();

        listing.AddedVia.Should().Be(ListingAddedVia.Manual);
        listing.AddedViaDetail.Should().BeNull();
    }

    [Fact]
    public void Create_stores_channel_provider()
    {
        var listing = NewListing(ListingAddedVia.Channel, " hostaway ");

        listing.AddedVia.Should().Be(ListingAddedVia.Channel);
        listing.AddedViaDetail.Should().Be("hostaway");
    }

    [Fact]
    public void MarkAddedVia_fills_in_manual_listings_only()
    {
        var listing = NewListing();
        listing.MarkAddedVia(ListingAddedVia.Channel, "guesty");
        listing.MarkAddedVia(ListingAddedVia.Excel);

        listing.AddedVia.Should().Be(ListingAddedVia.Channel);
        listing.AddedViaDetail.Should().Be("guesty");
    }
}
