using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingManagementTests
{
    private static Listing NewListing(Guid? landlordUserId = null) =>
        Listing.Create(
            landlordUserId: landlordUserId ?? Guid.NewGuid(),
            propertyType: PropertyType.Apartment,
            title: "Test listing",
            description: "A nice place to stay for a while.",
            monthlyRentCents: 300_000,
            bedrooms: 2,
            bathrooms: 1.5m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 500_000);

    [Fact]
    public void Defaults_to_owner_without_home_owner_or_broker_clause()
    {
        var listing = NewListing();

        listing.ManagerRole.Should().Be(ListingManagerRole.Owner);
        listing.HomeOwnerUserId.Should().BeNull();
        listing.IncludeBrokerClause.Should().BeFalse();
    }

    [Fact]
    public void Property_manager_requires_a_different_home_owner()
    {
        var listing = NewListing();
        var ownerId = Guid.NewGuid();

        listing.SetManagement(ListingManagerRole.PropertyManager, ownerId, includeBrokerClause: true);

        listing.ManagerRole.Should().Be(ListingManagerRole.PropertyManager);
        listing.HomeOwnerUserId.Should().Be(ownerId);
        listing.IncludeBrokerClause.Should().BeTrue();
    }

    [Fact]
    public void Property_manager_can_be_saved_without_home_owner()
    {
        var listing = NewListing();

        listing.SetManagement(ListingManagerRole.PropertyManager, null, false);

        listing.ManagerRole.Should().Be(ListingManagerRole.PropertyManager);
        listing.HomeOwnerUserId.Should().BeNull();
    }

    [Fact]
    public void Property_manager_cannot_submit_for_review_without_home_owner()
    {
        var listing = NewListing();
        listing.SetManagement(ListingManagerRole.PropertyManager, null, false);
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));
        listing.LockPreciseAddress(new Address("123 Main St", "San Francisco", "CA", "94102", "US"), "US-CA");

        var act = listing.SubmitForReview;

        act.Should().Throw<InvalidOperationException>().WithMessage("*home owner*");
    }

    [Fact]
    public void Property_manager_cannot_name_themselves_as_owner()
    {
        var landlordId = Guid.NewGuid();
        var listing = NewListing(landlordId);

        var act = () => listing.SetManagement(ListingManagerRole.PropertyManager, landlordId, false);

        act.Should().Throw<ArgumentException>().WithMessage("*different account*");
    }

    [Fact]
    public void Switching_back_to_owner_clears_home_owner()
    {
        var listing = NewListing();
        listing.SetManagement(ListingManagerRole.PropertyManager, Guid.NewGuid(), true);

        listing.SetManagement(ListingManagerRole.Owner, Guid.NewGuid(), false);

        listing.ManagerRole.Should().Be(ListingManagerRole.Owner);
        listing.HomeOwnerUserId.Should().BeNull();
        listing.IncludeBrokerClause.Should().BeFalse();
    }

    [Fact]
    public void Property_manager_with_home_owner_can_submit_for_review()
    {
        var listing = NewListing();
        listing.SetManagement(ListingManagerRole.PropertyManager, Guid.NewGuid(), false);
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));
        listing.LockPreciseAddress(new Address("123 Main St", "San Francisco", "CA", "94102", "US"), "US-CA");

        listing.SubmitForReview();

        listing.Status.Should().Be(ListingStatus.InReview);
    }
}
