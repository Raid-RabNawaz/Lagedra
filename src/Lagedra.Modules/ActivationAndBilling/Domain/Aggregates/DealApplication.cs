using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class DealApplication : AggregateRoot<Guid>
{
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
    public string? JurisdictionWarning { get; private set; }

    private DealApplication() { }

    public static DealApplication Submit(
        Guid listingId,
        Guid tenantUserId,
        Guid landlordUserId,
        DateOnly requestedCheckIn,
        DateOnly requestedCheckOut,
        Guid? partnerOrganizationId = null,
        bool isPartnerReferred = false)
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

        var application = new DealApplication
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            TenantUserId = tenantUserId,
            LandlordUserId = landlordUserId,
            RequestedCheckIn = requestedCheckIn,
            RequestedCheckOut = requestedCheckOut,
            StayDurationDays = duration,
            Status = DealApplicationStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PartnerOrganizationId = partnerOrganizationId,
            IsPartnerReferred = isPartnerReferred
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
