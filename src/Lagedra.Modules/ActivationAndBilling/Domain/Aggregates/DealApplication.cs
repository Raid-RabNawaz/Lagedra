using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class DealApplication : AggregateRoot<Guid>
{
    /// <summary>
    /// Hard cap on the tenant's optional cover note. Mirrors the textarea's
    /// `maxLength` on the apply dialog so client and server agree on what
    /// "too long" means, instead of one side silently truncating.
    /// </summary>
    public const int MessageMaxLength = 1000;

    public Guid ListingId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public Guid LandlordUserId { get; private set; }
    public DealApplicationStatus Status { get; private set; }
    public Guid? DealId { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }

    /// <summary>
    /// Predetermined deposit selected for the tenant's verification tier and
    /// snapshotted at request time (no longer entered by the host on approval).
    /// </summary>
    public long? DepositAmountCents { get; private set; }
    public long? InsuranceFeeCents { get; private set; }
    public long? FirstMonthRentCents { get; private set; }

    /// <summary>Platform service fee snapshotted at request time.</summary>
    public long? ServiceFeeCents { get; private set; }

    /// <summary>
    /// Total the tenant will be charged on host approval = deposit + first
    /// month rent + insurance + service fee. Snapshotted so the figure shown
    /// at request time is exactly what gets charged.
    /// </summary>
    public long? TotalPayableSnapshotCents { get; private set; }

    /// <summary>
    /// The verification tier resolved for the tenant at request time, which
    /// drove <see cref="DepositAmountCents"/>. Recorded for auditability.
    /// </summary>
    public TenantVerificationTier? TenantVerificationTierAtRequest { get; private set; }

    /// <summary>
    /// Human-readable explanation of why this deposit applies (e.g.
    /// "Partner guarantee applied"). Shown to the tenant and host.
    /// </summary>
    public string? DepositReason { get; private set; }

    public DateOnly RequestedCheckIn { get; private set; }
    public DateOnly RequestedCheckOut { get; private set; }
    public int StayDurationDays { get; private set; }
    public Guid? PartnerOrganizationId { get; private set; }
    public bool IsPartnerReferred { get; private set; }
    public DealApplicationSource Source { get; private set; }
    public string? JurisdictionWarning { get; private set; }

    /// <summary>
    /// Number of guests the tenant declared at submission time. Always
    /// at least 1 (the tenant counts as a guest), capped at the listing's
    /// <c>HouseRules.MaxGuests</c> by the submit command. Used by hosts
    /// when deciding whether to accept a request and surfaced verbatim
    /// on the Truth Surface so the booked headcount is auditable.
    /// </summary>
    public int GuestCount { get; private set; }

    /// <summary>
    /// Optional cover note from the tenant explaining why they want to
    /// book, travel context, who's coming, etc. — analogous to Airbnb's
    /// "Send the host a message" field. Capped at <see cref="MessageMaxLength"/>
    /// characters; longer payloads are rejected by the submit command
    /// rather than silently trimmed, so the tenant knows their note
    /// didn't get truncated mid-sentence.
    /// </summary>
    public string? Message { get; private set; }

    /// <summary>
    /// Set once both parties confirm the Truth Surface. Provides direct
    /// traceability from the booking record to its sealed legal snapshot.
    /// </summary>
    public Guid? TruthSurfaceSnapshotId { get; private set; }

    /// <summary>
    /// Phase 16.9 — Stripe payment-method id captured during the booking
    /// pre-flight (apply dialog SetupIntent step). When present and the
    /// BookingFlow.V2 flag is enabled the host's approve action immediately
    /// charges this card off-session, skipping the separate checkout page
    /// for the tenant.
    /// </summary>
    public string? StripePaymentMethodId { get; private set; }

    // --- Tenant Truth Surface consent (captured at request time) ---

    public bool TenantTruthSurfaceConsentGiven { get; private set; }
    public DateTime? TenantTruthSurfaceConsentAt { get; private set; }
    public string? TenantConsentIpAddress { get; private set; }
    public string? TenantConsentUserAgent { get; private set; }
    public string? TenantConsentVersion { get; private set; }

    // --- Host Truth Surface consent (captured at approval time) ---

    public bool HostTruthSurfaceConsentGiven { get; private set; }
    public DateTime? HostTruthSurfaceConsentAt { get; private set; }
    public string? HostConsentIpAddress { get; private set; }
    public string? HostConsentUserAgent { get; private set; }
    public string? HostConsentVersion { get; private set; }

    private DealApplication() { }

    public static DealApplication Submit(
        Guid listingId,
        Guid tenantUserId,
        Guid landlordUserId,
        DateOnly requestedCheckIn,
        DateOnly requestedCheckOut,
        int guestCount = 1,
        string? message = null,
        Guid? partnerOrganizationId = null,
        bool isPartnerReferred = false,
        DealApplicationSource source = DealApplicationSource.TenantSelfApply,
        string? stripePaymentMethodId = null,
        ReservationDepositSnapshot? depositSnapshot = null,
        TruthSurfaceConsentInput? tenantConsent = null)
    {
        if (requestedCheckOut <= requestedCheckIn)
        {
            throw new ArgumentException("Check-out must be after check-in.");
        }

        var duration = requestedCheckOut.DayNumber - requestedCheckIn.DayNumber;

        if (duration < 30)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedCheckOut), "Minimum stay is 30 days.");
        }

        if (duration > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedCheckOut), "Maximum stay is 180 days.");
        }

        if (guestCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(guestCount), "Guest count must be at least 1.");
        }

        // Trim + collapse the optional cover note rather than persisting
        // raw whitespace. Empty/whitespace-only notes are stored as null
        // so consumers can use a simple null check to decide whether to
        // render the "tenant message" section.
        var normalisedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        if (normalisedMessage is { Length: > MessageMaxLength })
        {
            throw new ArgumentOutOfRangeException(
                nameof(message),
                $"Message must be {MessageMaxLength} characters or fewer.");
        }

        var application = new DealApplication
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            TenantUserId = tenantUserId,
            LandlordUserId = landlordUserId,
            RequestedCheckIn = requestedCheckIn,
            RequestedCheckOut = requestedCheckOut,
            StayDurationDays = duration,
            GuestCount = guestCount,
            Message = normalisedMessage,
            Status = DealApplicationStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PartnerOrganizationId = partnerOrganizationId,
            IsPartnerReferred = isPartnerReferred,
            Source = source,
            StripePaymentMethodId = string.IsNullOrWhiteSpace(stripePaymentMethodId)
                ? null
                : stripePaymentMethodId,
        };

        if (depositSnapshot is not null)
        {
            application.ApplyDepositSnapshot(depositSnapshot);
        }

        if (tenantConsent is { Given: true })
        {
            application.RecordTenantConsent(tenantConsent);
        }

        application.AddDomainEvent(new ApplicationSubmittedEvent(
            application.Id, listingId, tenantUserId));

        return application;
    }

    /// <summary>
    /// Snapshots the predetermined deposit + quoted fees + resolved tier onto
    /// the application at request time. Idempotent values; safe to call once
    /// during <see cref="Submit"/>.
    /// </summary>
    public void ApplyDepositSnapshot(ReservationDepositSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.DepositAmountCents < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(snapshot), "Deposit must be non-negative.");
        }

        DepositAmountCents = snapshot.DepositAmountCents;
        FirstMonthRentCents = snapshot.FirstMonthRentCents;
        InsuranceFeeCents = snapshot.InsuranceFeeCents;
        ServiceFeeCents = snapshot.ServiceFeeCents;
        TotalPayableSnapshotCents = snapshot.TotalPayableCents;
        TenantVerificationTierAtRequest = snapshot.Tier;
        DepositReason = snapshot.DepositReason;
    }

    /// <summary>
    /// Records the tenant's Truth Surface consent + audit metadata gathered at
    /// request time. The host's consent is recorded later in <see cref="Approve"/>.
    /// </summary>
    public void RecordTenantConsent(TruthSurfaceConsentInput consent)
    {
        ArgumentNullException.ThrowIfNull(consent);

        if (!consent.Given)
        {
            throw new InvalidOperationException("Tenant Truth Surface consent was not given.");
        }

        TenantTruthSurfaceConsentGiven = true;
        TenantTruthSurfaceConsentAt = DateTime.UtcNow;
        TenantConsentVersion = consent.ConsentVersion;
        TenantConsentIpAddress = consent.IpAddress;
        TenantConsentUserAgent = consent.UserAgent;
    }

    /// <summary>
    /// Host accepts the request. No deposit input — the predetermined deposit
    /// and fees were snapshotted at request time. Records the host's Truth
    /// Surface consent (the seal + off-session charge follow). Returns the new
    /// deal id.
    /// </summary>
    public Guid Approve(
        string? jurisdictionWarning = null,
        TruthSurfaceConsentInput? hostConsent = null)
    {
        if (Status != DealApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve application in status '{Status}'.");
        }

        if (DepositAmountCents is null or < 0)
        {
            throw new InvalidOperationException(
                "Cannot approve an application without a deposit snapshot. " +
                "The reservation request must capture the predetermined deposit first.");
        }

        DealId = Guid.NewGuid();
        Status = DealApplicationStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        JurisdictionWarning = jurisdictionWarning;

        if (hostConsent is { Given: true })
        {
            RecordHostConsent(hostConsent);
        }

        AddDomainEvent(new ApplicationApprovedEvent(
            Id, DealId.Value, ListingId, LandlordUserId, TenantUserId));

        return DealId.Value;
    }

    /// <summary>
    /// Records the host's Truth Surface consent + audit metadata at approval
    /// time. Called from <see cref="Approve"/>.
    /// </summary>
    public void RecordHostConsent(TruthSurfaceConsentInput consent)
    {
        ArgumentNullException.ThrowIfNull(consent);

        if (!consent.Given)
        {
            throw new InvalidOperationException("Host Truth Surface consent was not given.");
        }

        HostTruthSurfaceConsentGiven = true;
        HostTruthSurfaceConsentAt = DateTime.UtcNow;
        HostConsentVersion = consent.ConsentVersion;
        HostConsentIpAddress = consent.IpAddress;
        HostConsentUserAgent = consent.UserAgent;
    }

    /// <summary>
    /// Host accepted and the Truth Surface sealed, but the off-session charge
    /// failed. The booking is held in <see cref="DealApplicationStatus.PaymentFailed"/>
    /// so the tenant can update their card and retry. Idempotent — a repeat call
    /// on an already-failed booking does not re-raise the notification event.
    /// </summary>
    public void MarkPaymentFailed(string? reason = null)
    {
        if (Status is not (DealApplicationStatus.Approved or DealApplicationStatus.PaymentFailed))
        {
            throw new InvalidOperationException(
                $"Cannot mark payment failed in status '{Status}'.");
        }

        if (Status == DealApplicationStatus.PaymentFailed)
        {
            return;
        }

        Status = DealApplicationStatus.PaymentFailed;

        AddDomainEvent(new BookingPaymentFailedEvent(
            Id,
            DealId ?? Id,
            ListingId,
            TenantUserId,
            LandlordUserId,
            string.IsNullOrWhiteSpace(reason) ? "payment-failed" : reason));
    }

    /// <summary>
    /// Clears a prior payment failure so a retry charge can run against the
    /// same sealed snapshot. Returns the booking to <see cref="DealApplicationStatus.Approved"/>.
    /// </summary>
    public void ClearPaymentFailure()
    {
        if (Status is not (DealApplicationStatus.PaymentFailed or DealApplicationStatus.Approved))
        {
            throw new InvalidOperationException(
                $"Cannot clear payment failure in status '{Status}'.");
        }

        Status = DealApplicationStatus.Approved;
    }

    /// <summary>
    /// Marks a still-pending request as expired (lapsed before the host
    /// decided). Only valid from <see cref="DealApplicationStatus.Pending"/>.
    /// </summary>
    public void MarkExpired()
    {
        if (Status != DealApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot expire application in status '{Status}'.");
        }

        Status = DealApplicationStatus.Expired;
        DecidedAt = DateTime.UtcNow;

        AddDomainEvent(new ApplicationExpiredEvent(
            Id, ListingId, LandlordUserId, TenantUserId));
    }

    public void Reject()
    {
        if (Status != DealApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject application in status '{Status}'.");
        }

        Status = DealApplicationStatus.Rejected;
        DecidedAt = DateTime.UtcNow;

        AddDomainEvent(new ApplicationRejectedEvent(
            Id, ListingId, LandlordUserId, TenantUserId));
    }

    /// <summary>
    /// Auto-rejects a pending request because the host accepted a different,
    /// date-overlapping request for the same listing. Ends in
    /// <see cref="DealApplicationStatus.Rejected"/> like a manual decline, but
    /// raises <see cref="ApplicationSupersededEvent"/> so the tenant is told the
    /// real reason rather than that the host declined them. Only valid from
    /// <see cref="DealApplicationStatus.Pending"/>.
    /// </summary>
    public void RejectAsSuperseded()
    {
        if (Status != DealApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot supersede application in status '{Status}'.");
        }

        Status = DealApplicationStatus.Rejected;
        DecidedAt = DateTime.UtcNow;

        AddDomainEvent(new ApplicationSupersededEvent(
            Id, ListingId, LandlordUserId, TenantUserId));
    }

    /// <summary>
    /// True when this request's stay overlaps the given date window. Stays are
    /// half-open intervals — the check-out day is a departure day, so a request
    /// that checks out on the same day another checks in does NOT overlap.
    /// </summary>
    public bool OverlapsWith(DateOnly checkIn, DateOnly checkOut) =>
        RequestedCheckIn < checkOut && checkIn < RequestedCheckOut;

    public void LinkTruthSurface(Guid snapshotId)
    {
        if (snapshotId == Guid.Empty)
        {
            throw new ArgumentException("Snapshot id must be non-empty.", nameof(snapshotId));
        }

        if (TruthSurfaceSnapshotId is not null && TruthSurfaceSnapshotId != snapshotId)
        {
            throw new InvalidOperationException(
                $"Application '{Id}' is already linked to a different Truth Surface snapshot.");
        }

        TruthSurfaceSnapshotId = snapshotId;
    }

    public void Cancel(
        Guid cancelledByUserId,
        string reason,
        bool isAutoCancel,
        long refundAmountCents,
        long insuranceRefundCents)
    {
        if (Status is DealApplicationStatus.Cancelled
            or DealApplicationStatus.Rejected
            or DealApplicationStatus.Expired)
        {
            throw new InvalidOperationException($"Cannot cancel application in status '{Status}'.");
        }

        Status = DealApplicationStatus.Cancelled;
        DecidedAt = DateTime.UtcNow;

        AddDomainEvent(new BookingCancelledEvent(
            DealId ?? Id, ListingId, cancelledByUserId, reason,
            isAutoCancel, refundAmountCents, insuranceRefundCents));
    }
}
