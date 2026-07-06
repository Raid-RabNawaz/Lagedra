using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class DealPaymentConfirmation : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public long TotalTenantPaymentCents { get; private set; }
    public long TotalHostPlatformPaymentCents { get; private set; }
    public long FirstMonthRentCents { get; private set; }
    public long DepositAmountCents { get; private set; }
    public long InsuranceFeeCents { get; private set; }
    public long MonthlyProtocolFeeCents { get; private set; }

    /// <summary>
    /// Platform service fee paid by the tenant at checkout (snapshot of the
    /// rate in effect when the deal was sealed). Part of
    /// <see cref="TotalTenantPaymentCents"/>.
    /// </summary>
    public long ServiceFeeCents { get; private set; }
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
    public string? StripePaymentIntentId { get; private set; }
    public string? StripePaymentStatus { get; private set; }

    /// <summary>
    /// Truth Surface snapshot id this payment row was created from. Lets
    /// arbitration trace a Stripe payment back to the exact sealed agreement.
    /// </summary>
    public Guid? TruthSurfaceSnapshotId { get; private set; }

    // ---- Deposit return handshake (non-custodial, host-held) ----
    // Lagedra never holds the deposit; the host returns it directly after
    // move-out. A deal is only "Completed" once BOTH the host confirms the
    // deposit was returned and the tenant confirms it was received.

    /// <summary>When either party started the move-out / deposit-return handshake.</summary>
    public DateTime? MoveOutInitiatedAt { get; private set; }

    /// <summary>Which participant started move-out.</summary>
    public Guid? MoveOutInitiatedByUserId { get; private set; }

    /// <summary>When the host confirmed they returned the deposit to the tenant.</summary>
    public DateTime? HostConfirmedDepositReturnedAt { get; private set; }

    /// <summary>When the tenant confirmed they received their deposit back.</summary>
    public DateTime? TenantConfirmedDepositReceivedAt { get; private set; }

    /// <summary>Amount the host states they returned (net of agreed/arbitrated deductions).</summary>
    public long? DepositReturnAmountCents { get; private set; }

    /// <summary>How the host returned the deposit (e.g. bank transfer, Zelle, cash, PlatformStripe).</summary>
    public string? DepositReturnMethod { get; private set; }

    /// <summary>Optional host note attached to the deposit return (reference, breakdown, etc.).</summary>
    public string? DepositReturnNote { get; private set; }

    /// <summary>Set once both parties have confirmed; this is the "deal completed" marker.</summary>
    public DateTime? DepositReturnSettledAt { get; private set; }

    /// <summary>Last time a deposit-return nudge was sent (throttles the reminder job).</summary>
    public DateTime? DepositReturnReminderSentAt { get; private set; }

    private DealPaymentConfirmation() { }

    public static DealPaymentConfirmation Create(
        Guid dealId,
        DealFinancials financials,
        IClock clock,
        int gracePeriodDays = 3,
        Guid? truthSurfaceSnapshotId = null)
    {
        ArgumentNullException.ThrowIfNull(financials);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new DealPaymentConfirmation
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            FirstMonthRentCents = financials.FirstMonthRentCents,
            DepositAmountCents = financials.DepositAmountCents,
            InsuranceFeeCents = financials.InsuranceFeeCents,
            MonthlyProtocolFeeCents = financials.MonthlyProtocolFeeCents,
            ServiceFeeCents = financials.ServiceFeeCents,
            TotalTenantPaymentCents = financials.TotalTenantPaymentCents,
            TotalHostPlatformPaymentCents = financials.TotalHostPlatformPaymentCents,
            Status = PaymentConfirmationStatus.Pending,
            GracePeriodExpiresAt = now.AddDays(gracePeriodDays),
            TruthSurfaceSnapshotId = truthSurfaceSnapshotId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetStripePaymentIntent(string paymentIntentId, string status, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        StripePaymentIntentId = paymentIntentId;
        StripePaymentStatus = status;
        UpdatedAt = clock.UtcNow;
    }

    public void ConfirmByStripe(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Status = PaymentConfirmationStatus.Confirmed;
        StripePaymentStatus = "succeeded";
        HostConfirmed = true;
        HostConfirmedAt = clock.UtcNow;
        HostPaidPlatform = true;
        HostPaidPlatformAt = clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        AddDomainEvent(new PaymentConfirmedEvent(DealId, clock.UtcNow));
    }

    public void FailByStripe(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        StripePaymentStatus = "failed";
        Status = PaymentConfirmationStatus.Failed;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Marks the off-session capture as failed (booking will not activate).
    /// Optionally records the Stripe status string. Idempotent — safe to call
    /// on an already-failed row when a retry charge fails again.
    /// </summary>
    public void MarkFailed(IClock clock, string? stripePaymentStatus = null)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Status = PaymentConfirmationStatus.Failed;
        StripePaymentStatus = stripePaymentStatus ?? StripePaymentStatus ?? "failed";
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Records that the tenant has provided a payment method but no money has
    /// moved yet (request submitted, awaiting host approval).
    /// </summary>
    public void MarkPaymentMethodProvided(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Status = PaymentConfirmationStatus.PaymentMethodProvided;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Records that an off-session capture is in flight (Stripe "processing"
    /// or "requires_capture"). The booking activates once it settles.
    /// </summary>
    public void MarkCapturePending(string paymentIntentId, string stripeStatus, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        StripePaymentIntentId = paymentIntentId;
        StripePaymentStatus = stripeStatus;
        Status = PaymentConfirmationStatus.CapturePending;
        UpdatedAt = clock.UtcNow;
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

        AddDomainEvent(new PaymentCancelledEvent(DealId, reason));
    }

    /// <summary>
    /// Opens the move-out / deposit-return handshake. Either participant may
    /// start it once the booking is active (payment confirmed). Idempotent:
    /// re-calling keeps the original initiator/timestamp.
    /// </summary>
    public void BeginMoveOut(Guid initiatedByUserId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PaymentConfirmationStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Move-out can only begin on a confirmed booking (status '{Status}').");
        }

        if (MoveOutInitiatedAt is not null)
        {
            return;
        }

        MoveOutInitiatedAt = clock.UtcNow;
        MoveOutInitiatedByUserId = initiatedByUserId;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Host confirms they returned the deposit directly to the tenant. Records
    /// the returned amount and how it was sent. Settles the handshake when the
    /// tenant has also confirmed receipt. No-op once already settled.
    /// </summary>
    public void ConfirmDepositReturnedByHost(
        long returnedAmountCents,
        string? method,
        string? note,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfNegative(returnedAmountCents);

        if (Status != PaymentConfirmationStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Cannot confirm a deposit return in status '{Status}'.");
        }

        if (DepositAmountCents <= 0)
        {
            throw new InvalidOperationException("This booking has no deposit to return.");
        }

        if (DepositReturnSettledAt is not null)
        {
            return;
        }

        HostConfirmedDepositReturnedAt ??= clock.UtcNow;
        DepositReturnAmountCents = returnedAmountCents;
        DepositReturnMethod = method;
        DepositReturnNote = note;
        UpdatedAt = clock.UtcNow;

        TrySettleDepositReturn(clock);
    }

    /// <summary>
    /// Tenant confirms they received their deposit back. Settles the handshake
    /// when the host has also confirmed the return. No-op once already settled.
    /// </summary>
    public void ConfirmDepositReceivedByTenant(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PaymentConfirmationStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Cannot confirm a deposit receipt in status '{Status}'.");
        }

        if (DepositAmountCents <= 0)
        {
            throw new InvalidOperationException("This booking has no deposit to return.");
        }

        if (DepositReturnSettledAt is not null)
        {
            return;
        }

        TenantConfirmedDepositReceivedAt ??= clock.UtcNow;
        UpdatedAt = clock.UtcNow;

        TrySettleDepositReturn(clock);
    }

    /// <summary>
    /// Admin / arbitration-enforced fallback: the platform recovered and
    /// returned the deposit (e.g. Stripe reverse-transfer). Records both sides
    /// as satisfied and settles the handshake. No-op once already settled.
    /// </summary>
    public void MarkDepositReturnedByPlatform(long returnedAmountCents, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfNegative(returnedAmountCents);

        if (DepositReturnSettledAt is not null)
        {
            return;
        }

        var now = clock.UtcNow;
        HostConfirmedDepositReturnedAt ??= now;
        TenantConfirmedDepositReceivedAt ??= now;
        DepositReturnAmountCents = returnedAmountCents;
        DepositReturnMethod = "PlatformStripe";
        UpdatedAt = now;

        TrySettleDepositReturn(clock);
    }

    private void TrySettleDepositReturn(IClock clock)
    {
        if (DepositReturnSettledAt is not null)
        {
            return;
        }

        if (HostConfirmedDepositReturnedAt is null || TenantConfirmedDepositReceivedAt is null)
        {
            return;
        }

        DepositReturnSettledAt = clock.UtcNow;
        AddDomainEvent(new DepositReturnSettledEvent(DealId, DepositReturnSettledAt.Value));
    }

    /// <summary>
    /// Whether an open (unsettled) deposit-return handshake is due for a nudge:
    /// the booking is confirmed, has a deposit, is not yet settled, and no
    /// reminder has been sent within <paramref name="reminderIntervalDays"/>.
    /// </summary>
    public bool DepositReturnReminderDue(IClock clock, int reminderIntervalDays)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PaymentConfirmationStatus.Confirmed
            || DepositAmountCents <= 0
            || DepositReturnSettledAt is not null)
        {
            return false;
        }

        return DepositReturnReminderSentAt is null
            || clock.UtcNow > DepositReturnReminderSentAt.Value.AddDays(reminderIntervalDays);
    }

    public void MarkDepositReturnReminderSent(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        DepositReturnReminderSentAt = clock.UtcNow;
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
