using Lagedra.Auth.Infrastructure.Jobs;
using Lagedra.Compliance.Infrastructure.Jobs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;
using Lagedra.Modules.Arbitration.Infrastructure.Jobs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Jobs;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Jobs;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Jobs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Jobs;
using Lagedra.Modules.Privacy.Infrastructure.Jobs;
using Lagedra.Modules.Reviews.Infrastructure.Jobs;
using Quartz;

namespace Lagedra.Worker.Scheduling;

// The composite sweep jobs. Each one owns a single Quartz trigger and runs
// its module tasks sequentially (see SequentialCompositeJob for failure
// semantics). Tasks keep their own logging, so per-task observability is
// unchanged — only the number of scheduled jobs shrinks.

/// <summary>
/// Every 15 minutes: fraud-flag SLA escalation, compliance signal
/// processing, and the insurance policy lifecycle (expire, then lapse).
/// Insurance moved from a 30-minute schedule — its transitions are one-shot
/// per record, so the tighter cadence only picks them up sooner.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class QuarterHourSweepJob(
    FraudFlagSlaMonitorJob fraudFlagSla,
    ComplianceSignalProcessorJob complianceSignals,
    InsuranceLifecycleJob insuranceLifecycle,
    ILogger<QuarterHourSweepJob> logger)
    : SequentialCompositeJob(logger)
{
    protected override IReadOnlyList<IJob> Children =>
        [fraudFlagSla, complianceSignals, insuranceLifecycle];
}

/// <summary>
/// Hourly: payment-confirmation timeouts (reminders + auto-cancel), stale
/// booking-request expiry, and the arbitration backlog SLA check. The
/// arbitration task re-raises an escalation on every run while cases are
/// overdue, so it must not run more often than hourly.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class HourlySweepJob(
    PaymentConfirmationTimeoutJob paymentTimeouts,
    ExpireStaleBookingRequestsJob staleBookingRequests,
    ArbitrationBacklogSlaJob arbitrationBacklog,
    ILogger<HourlySweepJob> logger)
    : SequentialCompositeJob(logger)
{
    protected override IReadOnlyList<IJob> Children =>
        [paymentTimeouts, staleBookingRequests, arbitrationBacklog];
}

/// <summary>
/// Every 6 hours: channel booking-update reconciliation and the compliance
/// violation scanner.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class SixHourSweepJob(
    ChannelBookingUpdateJob channelBookingUpdates,
    ComplianceScannerJob complianceScanner,
    ILogger<SixHourSweepJob> logger)
    : SequentialCompositeJob(logger)
{
    protected override IReadOnlyList<IJob> Children =>
        [channelBookingUpdates, complianceScanner];
}

/// <summary>
/// Twice daily (06:00/18:00 UTC): channel content sync and stay-review
/// publishing/reminders (reviews previously ran at 08:00/20:00; the window
/// checks are date-based, so the 2-hour shift is behavior-neutral).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class TwiceDailySweepJob(
    ChannelContentSyncJob channelContentSync,
    PublishExpiredStayReviewsJob publishStayReviews,
    ILogger<TwiceDailySweepJob> logger)
    : SequentialCompositeJob(logger)
{
    protected override IReadOnlyList<IJob> Children =>
        [channelContentSync, publishStayReviews];
}

/// <summary>
/// Nightly at 03:00 UTC (≈ 19:00–20:00 Pacific — a humane hour for the
/// reminder emails some tasks send): privacy housekeeping, partner
/// endorsement expiry, listing jurisdiction resolution, refresh-token
/// cleanup, deposit-return nudges, monthly rent check-ins, billing
/// reconciliation, host platform-fee enforcement, and OwnerRez OAuth token
/// renewal (its 30-day expiry only needs a daily check).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class NightlyMaintenanceJob(
    PrivacyMaintenanceJob privacyMaintenance,
    ExpirePartnerEndorsementsJob partnerEndorsements,
    JurisdictionResolutionJob jurisdictionResolution,
    RefreshTokenCleanupJob refreshTokenCleanup,
    DepositReturnJob depositReturns,
    RentCheckInJob rentCheckIns,
    BillingReconciliationJob billingReconciliation,
    HostPlatformPaymentEnforcementJob hostPaymentEnforcement,
    OwnerRezTokenRefreshJob ownerRezTokenRefresh,
    ILogger<NightlyMaintenanceJob> logger)
    : SequentialCompositeJob(logger)
{
    protected override IReadOnlyList<IJob> Children =>
    [
        privacyMaintenance,
        partnerEndorsements,
        jurisdictionResolution,
        refreshTokenCleanup,
        depositReturns,
        rentCheckIns,
        billingReconciliation,
        hostPaymentEnforcement,
        ownerRezTokenRefresh,
    ];
}
