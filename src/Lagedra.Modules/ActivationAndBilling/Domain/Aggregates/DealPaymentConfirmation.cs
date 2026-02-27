using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class DealPaymentConfirmation : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public long TotalTenantPaymentCents { get; private set; }
    public long TotalHostPlatformPaymentCents { get; private set; }
    public bool HostPaidPlatform { get; private set; }
    public DateTime? HostPaidPlatformAt { get; private set; }
    public bool HostConfirmed { get; private set; }
    public DateTime? HostConfirmedAt { get; private set; }
    public bool TenantDisputed { get; private set; }
    public DateTime? TenantDisputedAt { get; private set; }
    public string? DisputeReason { get; private set; }
    public Guid? DisputeEvidenceManifestId { get; private set; }
    public PaymentConfirmationStatus Status { get; private set; }
    public DateTime GracePeriodExpiresAt { get; private set; }
    public DateTime? ReminderSentAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? HostPlatformReminderSentAt { get; private set; }

    private DealPaymentConfirmation() { }

    public static DealPaymentConfirmation Create(
        Guid dealId,
        long totalTenantPaymentCents,
        long totalHostPlatformPaymentCents,
        IClock clock,
        int gracePeriodDays = 3)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new DealPaymentConfirmation
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            TotalTenantPaymentCents = totalTenantPaymentCents,
            TotalHostPlatformPaymentCents = totalHostPlatformPaymentCents,
            Status = PaymentConfirmationStatus.Pending,
            GracePeriodExpiresAt = now.AddDays(gracePeriodDays),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void ConfirmHostPlatformPayment(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (!HostConfirmed)
        {
            throw new InvalidOperationException(
                "Host must confirm tenant payment before paying the platform.");
        }

        if (HostPaidPlatform)
        {
            throw new InvalidOperationException("Host has already paid the platform.");
        }

        HostPaidPlatform = true;
        HostPaidPlatformAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public void ConfirmByHost(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PaymentConfirmationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot confirm payment in status '{Status}'.");
        }

        HostConfirmed = true;
        HostConfirmedAt = clock.UtcNow;
        Status = PaymentConfirmationStatus.Confirmed;
        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PaymentConfirmedEvent(DealId, HostConfirmedAt.Value));
    }

    public void DisputeByTenant(
        Guid tenantUserId,
        string reason,
        Guid? evidenceManifestId,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is not (PaymentConfirmationStatus.Pending or PaymentConfirmationStatus.Rejected))
        {
            throw new InvalidOperationException(
                $"Cannot dispute payment in status '{Status}'.");
        }

        TenantDisputed = true;
        TenantDisputedAt = clock.UtcNow;
        DisputeReason = reason;
        DisputeEvidenceManifestId = evidenceManifestId;
        Status = PaymentConfirmationStatus.Disputed;
        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PaymentDisputedEvent(DealId, tenantUserId, reason, evidenceManifestId));
    }

    public void ResolveDispute(bool paymentValid, Guid resolvedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PaymentConfirmationStatus.Disputed)
        {
            throw new InvalidOperationException(
                $"Cannot resolve dispute in status '{Status}'.");
        }

        Status = paymentValid
            ? PaymentConfirmationStatus.Confirmed
            : PaymentConfirmationStatus.Rejected;

        if (paymentValid)
        {
            HostConfirmed = true;
            HostConfirmedAt = clock.UtcNow;
        }

        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PaymentDisputeResolvedEvent(DealId, paymentValid, resolvedBy));
    }

    public void Cancel(string reason, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is PaymentConfirmationStatus.Cancelled)
        {
            throw new InvalidOperationException("Payment confirmation is already cancelled.");
        }

        Status = PaymentConfirmationStatus.Cancelled;
        CancelledAt = clock.UtcNow;
        CancellationReason = reason;
        UpdatedAt = clock.UtcNow;
    }

    public void MarkReminderSent(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ReminderSentAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public void MarkHostPlatformReminderSent(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        HostPlatformReminderSentAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;
    }

    public bool IsGracePeriodExpired(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return Status == PaymentConfirmationStatus.Pending && clock.UtcNow > GracePeriodExpiresAt;
    }

    public bool NeedsReminder(IClock clock, int reminderAfterDays)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status != PaymentConfirmationStatus.Pending || ReminderSentAt is not null)
        {
            return false;
        }

        return clock.UtcNow > CreatedAt.AddDays(reminderAfterDays);
    }

    public bool ShouldAutoCancel(IClock clock, int autoCancelAfterDays)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status != PaymentConfirmationStatus.Pending)
        {
            return false;
        }

        return clock.UtcNow > CreatedAt.AddDays(autoCancelAfterDays);
    }

    public bool HostNeedsPlatformPaymentReminder(IClock clock, int reminderIntervalDays)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status != PaymentConfirmationStatus.Confirmed || !HostConfirmed || HostPaidPlatform)
        {
            return false;
        }

        var lastReminder = HostPlatformReminderSentAt ?? HostConfirmedAt;
        return lastReminder is not null && clock.UtcNow > lastReminder.Value.AddDays(reminderIntervalDays);
    }

    public bool HostShouldBeSuspended(IClock clock, int suspendAfterDays)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status != PaymentConfirmationStatus.Confirmed || !HostConfirmed || HostPaidPlatform)
        {
            return false;
        }

        return HostConfirmedAt is not null && clock.UtcNow > HostConfirmedAt.Value.AddDays(suspendAfterDays);
    }
}
