using System;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class ListingCustomLeaseAgreementTests
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

    private static Listing SubmittableListing()
    {
        var listing = NewListing();
        listing.LockPreciseAddress(
            new Address("123 Main St", "San Francisco", "CA", "94102", "US"), "US-CA");
        listing.SetApproxLocation(new GeoPoint(37.7749, -122.4194));
        return listing;
    }

    private static CustomLeaseDocument SampleDocument() =>
        CustomLeaseDocument.Create(
            storageKey: "lease-documents/abc/lease.pdf",
            fileName: "lease.pdf",
            contentType: "application/pdf",
            sizeBytes: 1024,
            contentHash: "ABC123",
            uploadedAtUtc: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Defaults_to_the_lagedra_template()
    {
        var listing = NewListing();

        listing.LeaseAgreementSource.Should().Be(LeaseAgreementSource.LagedraTemplate);
        listing.CustomLeaseDocument.Should().BeNull();
    }

    [Fact]
    public void Attaching_a_document_records_its_metadata()
    {
        var listing = NewListing();

        listing.AttachCustomLeaseDocument(SampleDocument());

        listing.CustomLeaseDocument.Should().NotBeNull();
        listing.CustomLeaseDocument!.FileName.Should().Be("lease.pdf");
        listing.CustomLeaseDocument.ContentHash.Should().Be("ABC123");
    }

    [Fact]
    public void Removing_the_document_falls_back_to_the_lagedra_template()
    {
        var listing = NewListing();
        listing.AttachCustomLeaseDocument(SampleDocument());
        listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);

        listing.RemoveCustomLeaseDocument();

        listing.CustomLeaseDocument.Should().BeNull();
        listing.LeaseAgreementSource.Should().Be(LeaseAgreementSource.LagedraTemplate);
    }

    [Fact]
    public void Switching_back_to_the_template_keeps_the_uploaded_document()
    {
        var listing = NewListing();
        listing.AttachCustomLeaseDocument(SampleDocument());
        listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);

        listing.SetLeaseAgreementSource(LeaseAgreementSource.LagedraTemplate);

        // Toggling the choice must not be destructive — the host may flip back.
        listing.CustomLeaseDocument.Should().NotBeNull();
    }

    [Fact]
    public void Cannot_submit_for_review_promising_a_host_lease_that_was_never_uploaded()
    {
        var listing = SubmittableListing();
        listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);

        var act = listing.SubmitForReview;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Upload your lease agreement*");
    }

    [Fact]
    public void Submits_for_review_once_the_host_lease_is_uploaded()
    {
        var listing = SubmittableListing();
        listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);
        listing.AttachCustomLeaseDocument(SampleDocument());

        listing.SubmitForReview();

        listing.Status.Should().Be(ListingStatus.InReview);
    }

    [Fact]
    public void Cannot_change_the_lease_choice_while_the_listing_is_under_review()
    {
        var listing = SubmittableListing();
        listing.SubmitForReview();

        var act = () => listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be edited*");
    }
}
