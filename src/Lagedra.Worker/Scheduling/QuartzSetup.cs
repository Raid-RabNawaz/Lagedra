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
using Lagedra.Worker.Orchestration;
using Quartz;

namespace Lagedra.Worker.Scheduling;

internal static class QuartzSetup
{
    public static IServiceCollection AddQuartzScheduling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        services.AddQuartz(q =>
        {
            // Unique instance id per task — required for clustering.
            q.SchedulerId = "AUTO";

            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);

            q.UsePersistentStore(store =>
            {
                store.UsePostgres(connectionString);
                store.UseNewtonsoftJsonSerializer();
                // ECS rolling deploys run the old and new worker side by side
                // for ~1 minute. Without clustering, two schedulers on the
                // same store both fire due triggers (duplicate job runs) and
                // can corrupt each other's trigger state. With clustering the
                // instances coordinate through qrtz_locks/qrtz_scheduler_state
                // and every trigger fires exactly once.
                store.UseClustering();
            });

            JobRegistry.RegisterAllJobs(q);
        });

        // Module tasks executed inside the composite sweep jobs. They are
        // plain scoped services here — not Quartz jobs — so the composites
        // can constructor-inject them; JobRegistry only schedules the
        // composites themselves.
        services.AddScoped<FraudFlagSlaMonitorJob>();
        services.AddScoped<ComplianceSignalProcessorJob>();
        services.AddScoped<InsuranceLifecycleJob>();
        services.AddScoped<PaymentConfirmationTimeoutJob>();
        services.AddScoped<ExpireStaleBookingRequestsJob>();
        services.AddScoped<ArbitrationBacklogSlaJob>();
        services.AddScoped<ChannelBookingUpdateJob>();
        services.AddScoped<ComplianceScannerJob>();
        services.AddScoped<ChannelContentSyncJob>();
        services.AddScoped<PublishExpiredStayReviewsJob>();
        services.AddScoped<PrivacyMaintenanceJob>();
        services.AddScoped<ExpirePartnerEndorsementsJob>();
        services.AddScoped<JurisdictionResolutionJob>();
        services.AddScoped<RefreshTokenCleanupJob>();
        services.AddScoped<DepositReturnJob>();
        services.AddScoped<RentCheckInJob>();
        services.AddScoped<BillingReconciliationJob>();
        services.AddScoped<HostPlatformPaymentEnforcementJob>();
        services.AddScoped<OwnerRezTokenRefreshJob>();

        // Must be registered before AddQuartzHostedService: it creates the
        // qrtz_* tables when missing and purges stale job keys, both of which
        // have to happen before the scheduler starts.
        services.AddHostedService<QuartzBootstrapService>();
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddHostedService<HealthOrchestrator>();

        return services;
    }
}
