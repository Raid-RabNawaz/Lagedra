using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
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
    public long? DepositAmountCents { get; private set; }
    public long? InsuranceFeeCents { get; private set; }
    public long? FirstMonthRentCents { get; private set; }
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
        string? stripePaymentMethodId = null)
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

        application.AddDomainEvent(new ApplicationSubmittedEvent(
            application.Id, listingId, tenantUserId));

        return application;
    }

    public Guid Approve(
        long depositAmountCents,
        long insuranceFeeCents,
        long firstMonthRentCents,
        string? jurisdictionWarning)
    {
        if (Status != DealApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve application in status '{Status}'.");
        }

        if (depositAmountCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depositAmountCents), "Deposit must be positive.");
        }

        DealId = Guid.NewGuid();
        Status = DealApplicationStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        DepositAmountCents = depositAmountCents;
        InsuranceFeeCents = insuranceFeeCents;
        FirstMonthRentCents = firstMonthRentCents;
        JurisdictionWarning = jurisdictionWarning;

        AddDomainEvent(new ApplicationApprovedEvent(
            Id, DealId.Value, ListingId, LandlordUserId, TenantUserId));

        return DealId.Value;
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
        if (Status is DealApplicationStatus.Cancelled or DealApplicationStatus.Rejected)
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
