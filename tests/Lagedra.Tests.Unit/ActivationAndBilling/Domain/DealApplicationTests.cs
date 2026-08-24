using System;
using System.Linq;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.SharedKernel.Integration;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class DealApplicationTests
{
    private static readonly Guid Listing = Guid.NewGuid();
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Landlord = Guid.NewGuid();

    private static readonly DateOnly CheckIn = new(2026, 1, 1);
    private static readonly DateOnly CheckOut = new(2026, 3, 1); // 59 days, within [30,180]

    private static ReservationDepositSnapshot Snapshot() =>
        new(
            Tier: TenantVerificationTier.BackgroundVerified,
            DepositAmountCents: 200_000,
            FirstMonthRentCents: 300_000,
            InsuranceFeeCents: 10_000,
            ServiceFeeCents: 5_000,
            DepositReason: "Verified tenant discount applied");

    private static TruthSurfaceConsentInput TenantConsent() =>
        new(Given: true, ConsentVersion: "ts-consent-v1", IpAddress: "1.2.3.4", UserAgent: "jest");

    private static DealApplication SubmitWithSnapshot() =>
        DealApplication.Submit(
            Listing, Tenant, Landlord, CheckIn, CheckOut,
            guestCount: 2,
            message: "Hello host",
            stripePaymentMethodId: "pm_123",
            depositSnapshot: Snapshot(),
            tenantConsent: TenantConsent());

    [Fact]
    public void Submit_snapshots_deposit_fees_and_tenant_consent()
    {
        var app = SubmitWithSnapshot();

        app.Status.Should().Be(DealApplicationStatus.Pending);
        app.DepositAmountCents.Should().Be(200_000);
        app.FirstMonthRentCents.Should().Be(300_000);
        app.InsuranceFeeCents.Should().Be(10_000);
        app.ServiceFeeCents.Should().Be(5_000);
        app.TotalPayableSnapshotCents.Should().Be(515_000);
        app.TenantVerificationTierAtRequest.Should().Be(TenantVerificationTier.BackgroundVerified);
        app.DepositReason.Should().Be("Verified tenant discount applied");

        app.TenantTruthSurfaceConsentGiven.Should().BeTrue();
        app.TenantConsentVersion.Should().Be("ts-consent-v1");
        app.TenantConsentIpAddress.Should().Be("1.2.3.4");
        app.StripePaymentMethodId.Should().Be("pm_123");

        app.DomainEvents.OfType<ApplicationSubmittedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Approve_requires_owner_consent_when_flagged_and_does_not_mutate()
    {
        var owner = Guid.NewGuid();
        var app = SubmitWithSnapshot();
        app.RequireOwnerConsent(owner);

        var act = () => app.Approve(hostConsent: TenantConsent());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*home owner must consent*");
        app.Status.Should().Be(DealApplicationStatus.Pending);
        app.DealId.Should().BeNull();
        app.DomainEvents.OfType<ApplicationApprovedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void RecordOwnerConsent_then_approve_succeeds()
    {
        var owner = Guid.NewGuid();
        var app = SubmitWithSnapshot();
        app.RequireOwnerConsent(owner);
        app.RecordOwnerConsent(
            owner,
            new TruthSurfaceConsentInput(true, "owner-tenancy-consent-v1", "9.9.9.9", "owner-ua"));

        app.OwnerTenancyConsentGiven.Should().BeTrue();
        app.OwnerConsentVersion.Should().Be("owner-tenancy-consent-v1");
        app.DomainEvents.OfType<OwnerTenancyConsentGivenEvent>().Should().ContainSingle();

        var dealId = app.Approve(hostConsent: TenantConsent());

        app.Status.Should().Be(DealApplicationStatus.Approved);
        app.DealId.Should().Be(dealId);
    }

    [Fact]
    public void RecordOwnerConsent_is_idempotent()
    {
        var owner = Guid.NewGuid();
        var app = SubmitWithSnapshot();
        app.RequireOwnerConsent(owner);
        app.RecordOwnerConsent(owner, TenantConsent());
        app.ClearDomainEvents();

        app.RecordOwnerConsent(owner, TenantConsent());

        app.OwnerTenancyConsentGiven.Should().BeTrue();
        app.DomainEvents.OfType<OwnerTenancyConsentGivenEvent>().Should().BeEmpty();
    }

    [Fact]
    public void DeclineOwnerConsent_rejects_and_blocks_approve()
    {
        var owner = Guid.NewGuid();
        var app = SubmitWithSnapshot();
        app.RequireOwnerConsent(owner);
        app.DeclineOwnerConsent(owner);

        app.Status.Should().Be(DealApplicationStatus.Rejected);
        app.OwnerTenancyConsentDeclined.Should().BeTrue();
        app.DomainEvents.OfType<OwnerTenancyConsentDeclinedEvent>().Should().ContainSingle();
        app.DomainEvents.OfType<ApplicationRejectedEvent>().Should().BeEmpty();

        var act = () => app.Approve(hostConsent: TenantConsent());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequireOwnerConsent_rejects_same_account_as_property_manager()
    {
        var app = SubmitWithSnapshot();

        var act = () => app.RequireOwnerConsent(Landlord);

        act.Should().Throw<ArgumentException>();
        app.OwnerConsentRequired.Should().BeFalse();
    }

    [Fact]
    public void Approve_records_host_consent_and_raises_approved_event()
    {
        var app = SubmitWithSnapshot();
        var host = new TruthSurfaceConsentInput(true, "ts-consent-v1", "5.6.7.8", "host-ua");

        var dealId = app.Approve(hostConsent: host);

        app.Status.Should().Be(DealApplicationStatus.Approved);
        app.DealId.Should().Be(dealId);
        app.HostTruthSurfaceConsentGiven.Should().BeTrue();
        app.HostConsentVersion.Should().Be("ts-consent-v1");
        app.DomainEvents.OfType<ApplicationApprovedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Double_approve_is_rejected_so_no_double_charge()
    {
        var app = SubmitWithSnapshot();
        app.Approve(hostConsent: TenantConsent());

        var act = () => app.Approve(hostConsent: TenantConsent());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_without_deposit_snapshot_is_rejected()
    {
        var app = DealApplication.Submit(Listing, Tenant, Landlord, CheckIn, CheckOut);

        var act = () => app.Approve(hostConsent: TenantConsent());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkPaymentFailed_sets_status_and_raises_event_once()
    {
        var app = SubmitWithSnapshot();
        app.Approve(hostConsent: TenantConsent());
        app.ClearDomainEvents();

        app.MarkPaymentFailed("card_declined");

        app.Status.Should().Be(DealApplicationStatus.PaymentFailed);
        var failed = app.DomainEvents.OfType<BookingPaymentFailedEvent>().Should().ContainSingle().Subject;
        failed.Reason.Should().Be("card_declined");

        // Idempotent: a second failure (e.g. retry also fails before status reset)
        // does not re-raise the notification event.
        app.ClearDomainEvents();
        app.MarkPaymentFailed("card_declined_again");
        app.DomainEvents.OfType<BookingPaymentFailedEvent>().Should().BeEmpty();
        app.Status.Should().Be(DealApplicationStatus.PaymentFailed);
    }

    [Fact]
    public void ClearPaymentFailure_allows_retry_back_to_approved()
    {
        var app = SubmitWithSnapshot();
        app.Approve(hostConsent: TenantConsent());
        app.MarkPaymentFailed();

        app.ClearPaymentFailure();

        app.Status.Should().Be(DealApplicationStatus.Approved);
    }

    [Fact]
    public void Submit_rejects_consent_input_that_was_not_given()
    {
        var notGiven = new TruthSurfaceConsentInput(false, "ts-consent-v1", null, null);

        // A "not given" consent is simply ignored at submit (only Given:true records).
        var app = DealApplication.Submit(
            Listing, Tenant, Landlord, CheckIn, CheckOut,
            depositSnapshot: Snapshot(),
            tenantConsent: notGiven);

        app.TenantTruthSurfaceConsentGiven.Should().BeFalse();
    }

    [Fact]
    public void MarkExpired_sets_status_and_raises_expired_event()
    {
        var app = SubmitWithSnapshot();
        app.ClearDomainEvents();

        app.MarkExpired();

        app.Status.Should().Be(DealApplicationStatus.Expired);
        app.DecidedAt.Should().NotBeNull();
        app.DomainEvents.OfType<ApplicationExpiredEvent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkExpired_only_valid_from_pending()
    {
        var app = SubmitWithSnapshot();
        app.Approve(hostConsent: TenantConsent());

        var act = () => app.MarkExpired();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RejectAsSuperseded_rejects_and_raises_superseded_event()
    {
        var app = SubmitWithSnapshot();
        app.ClearDomainEvents();

        app.RejectAsSuperseded();

        app.Status.Should().Be(DealApplicationStatus.Rejected);
        app.DecidedAt.Should().NotBeNull();
        app.DomainEvents.OfType<ApplicationSupersededEvent>().Should().ContainSingle();
        // Distinct from a manual host decline so tenant-facing copy can differ.
        app.DomainEvents.OfType<ApplicationRejectedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void RejectAsSuperseded_only_valid_from_pending()
    {
        var app = SubmitWithSnapshot();
        app.Approve(hostConsent: TenantConsent());

        var act = () => app.RejectAsSuperseded();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    // Same window overlaps.
    [InlineData(2026, 1, 1, 2026, 3, 1, true)]
    // Fully contained.
    [InlineData(2026, 1, 15, 2026, 2, 15, true)]
    // Partial overlap at the tail.
    [InlineData(2026, 2, 1, 2026, 4, 1, true)]
    // Adjacent: other checks out exactly when this checks in -> no overlap (half-open).
    [InlineData(2025, 11, 1, 2026, 1, 1, false)]
    // Adjacent: other checks in exactly when this checks out -> no overlap.
    [InlineData(2026, 3, 1, 2026, 4, 1, false)]
    // Entirely after.
    [InlineData(2026, 6, 1, 2026, 8, 1, false)]
    public void OverlapsWith_uses_half_open_intervals(
        int y1, int m1, int d1, int y2, int m2, int d2, bool expected)
    {
        // This application is CheckIn..CheckOut = 2026-01-01 .. 2026-03-01.
        var app = SubmitWithSnapshot();

        app.OverlapsWith(new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2))
            .Should().Be(expected);
    }
}
