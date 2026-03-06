using Lagedra.Auth.Infrastructure.Jobs;
using Lagedra.Compliance.Infrastructure.Jobs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Jobs;
using Lagedra.Modules.Arbitration.Infrastructure.Jobs;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Jobs;
using Lagedra.Modules.Evidence.Infrastructure.Jobs;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Jobs;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Jobs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Jobs;
using Lagedra.Modules.Notifications.Infrastructure.Jobs;
using Lagedra.Modules.Privacy.Infrastructure.Jobs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Jobs;
using Lagedra.TruthSurface.Infrastructure.Jobs;
using Quartz;

namespace Lagedra.Worker.Scheduling;

internal static class JobRegistry
{
    public static void RegisterAllJobs(IServiceCollectionQuartzConfigurator q)
    {
        ArgumentNullException.ThrowIfNull(q);

        // Auth
        Register<RefreshTokenCleanupJob>(q, "0 0 3 * * ?");

        // ActivationAndBilling
        Register<BillingReconciliationJob>(q, "0 0 4 * * ?");
        Register<PaymentConfirmationTimeoutJob>(q, "0 0 * * * ?");
        Register<HostPlatformPaymentEnforcementJob>(q, "0 0 8 * * ?");

        // InsuranceIntegration
        Register<InsurancePollerJob>(q, "0 0 * * * ?");
        Register<InsuranceUnknownSlaJob>(q, "0 */30 * * * ?");

        // IdentityAndVerification
        Register<FraudFlagSlaMonitorJob>(q, "0 */15 * * * ?");

        // ComplianceMonitoring
        Register<ComplianceScannerJob>(q, "0 0 */6 * * ?");

        // Compliance
        Register<ComplianceSignalProcessorJob>(q, "0 */15 * * * ?");

        // Arbitration
        Register<ArbitrationBacklogSlaJob>(q, "0 0 * * * ?");

        // JurisdictionPacks
        Register<PackEffectiveDateActivationJob>(q, "0 0 0 * * ?");

        // Evidence
        Register<MalwareScanPollingJob>(q, "0 */5 * * * ?");
        Register<EvidenceRetentionJob>(q, "0 0 2 * * ?");

        // Notifications
        Register<NotificationRetryJob>(q, "0 */10 * * * ?");
        Register<NotificationProcessingJob>(q, "0/30 * * * * ?");

        // Privacy
        Register<RetentionEnforcementJob>(q, "0 0 1 * * ?");
        Register<DataExportPurgeJob>(q, "0 0 5 * * ?");

        // AntiAbuseAndIntegrity
        Register<PatternDetectionSchedulerJob>(q, "0 0 */4 * * ?");

        // ListingAndLocation
        Register<JurisdictionResolutionJob>(q, "0 0 2 * * ?");

        // StructuredInquiry
        Register<InquiryIntegrityScanJob>(q, "0 0 6 * * ?");

        // TruthSurface
        Register<SnapshotVerificationJob>(q, "0 0 3 ? * SUN");
    }

    private static void Register<T>(IServiceCollectionQuartzConfigurator q, string cronExpression)
        where T : class, IJob
    {
        var name = typeof(T).Name;

        q.AddJob<T>(opts => opts.WithIdentity(name));
        q.AddTrigger(t => t
            .ForJob(name)
            .WithIdentity($"{name}-trigger")
            .WithCronSchedule(cronExpression));
    }
}
