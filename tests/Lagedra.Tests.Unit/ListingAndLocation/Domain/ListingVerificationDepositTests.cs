using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingVerificationDepositTests
{
    private const long Max = 500_000;

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
            maxDepositCents: Max);

    [Fact]
    public void Sets_valid_ordered_tier_deposits()
    {
        var listing = NewListing();

        listing.SetVerificationDeposits(
            unverifiedCents: 300_000,
            backgroundVerifiedCents: 200_000,
            partnerGuaranteedCents: 100_000);

        listing.DepositUnverifiedCents.Should().Be(300_000);
        listing.DepositBackgroundVerifiedCents.Should().Be(200_000);
        listing.DepositPartnerGuaranteedCents.Should().Be(100_000);
    }

    [Fact]
    public void Allows_partial_configuration()
    {
        var listing = NewListing();

        listing.SetVerificationDeposits(
            unverifiedCents: 250_000,
            backgroundVerifiedCents: null,
            partnerGuaranteedCents: null);

        listing.DepositUnverifiedCents.Should().Be(250_000);
        listing.DepositBackgroundVerifiedCents.Should().BeNull();
        listing.DepositPartnerGuaranteedCents.Should().BeNull();
    }

    [Fact]
    public void Rejects_tier_deposit_above_max()
    {
        var listing = NewListing();

        var act = () => listing.SetVerificationDeposits(
            unverifiedCents: Max + 1, backgroundVerifiedCents: null, partnerGuaranteedCents: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_negative_tier_deposit()
    {
        var listing = NewListing();

        var act = () => listing.SetVerificationDeposits(
            unverifiedCents: -1, backgroundVerifiedCents: null, partnerGuaranteedCents: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_background_above_unverified()
    {
        var listing = NewListing();

        var act = () => listing.SetVerificationDeposits(
            unverifiedCents: 100_000, backgroundVerifiedCents: 200_000, partnerGuaranteedCents: null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_partner_above_background()
    {
        var listing = NewListing();

        var act = () => listing.SetVerificationDeposits(
            unverifiedCents: 300_000, backgroundVerifiedCents: 100_000, partnerGuaranteedCents: 200_000);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Lowering_max_deposit_clears_now_invalid_tier_amounts()
    {
        var listing = NewListing();
        listing.SetVerificationDeposits(300_000, 200_000, 100_000);

        // Lowering the max deposit (via Update) must clear any now-out-of-range
        // tier amounts so the booking flow falls back to the new max instead.
        listing.Update(
            propertyType: PropertyType.Apartment,
            title: "Test listing",
            description: "A nice place to stay for a while.",
            monthlyRentCents: 300_000,
            bedrooms: 2,
            bathrooms: 1.5m,
            stayRange: new StayRange(30, 180),
            maxDepositCents: 150_000);

        listing.DepositUnverifiedCents.Should().BeNull();    // 300k > 150k -> cleared
        listing.DepositBackgroundVerifiedCents.Should().BeNull(); // 200k > 150k -> cleared
        listing.DepositPartnerGuaranteedCents.Should().Be(100_000); // still valid
    }
}
