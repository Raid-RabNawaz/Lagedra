using Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;
using Lagedra.Modules.Evidence.Infrastructure.Jobs;
using Lagedra.Modules.Notifications.Infrastructure.Jobs;
using Lagedra.TruthSurface.Infrastructure.Jobs;
using Quartz;

namespace Lagedra.Worker.Scheduling;

/// <summary>
/// The single source of truth for every scheduled job in the system.
/// Modules must NOT register jobs themselves (they used to via AddQuartz in
/// their registration extensions, which caused each job to run 2-3 times).
///
/// Most module tasks are not scheduled individually — they run inside the
/// composite sweep jobs (see CompositeSweepJobs.cs), which keeps the number
/// of scheduled jobs to the minimum. Standalone entries below either run far
/// more often than everything else, have their own alerting identity, or
/// have an external-API budget tied to their cadence.
/// </summary>
internal static class JobRegistry
{
    internal sealed record ScheduledJob(Type JobType, string CronExpression)
    {
        public string Name => JobType.Name;
    }

    public static IReadOnlyList<ScheduledJob> Jobs { get; } =
    [
        // Core notification delivery + retry backoff (every 30 s).
        new(typeof(NotificationProcessingJob), "0/30 * * * * ?"),

        // Evidence malware scanning (every 5 min). Standalone so it keeps its
        // own identity in HealthOrchestrator's critical-job alerting.
        new(typeof(MalwareScanPollingJob), "0 */5 * * * ?"),

        // Composite sweeps — one trigger each, several module tasks inside.
        new(typeof(QuarterHourSweepJob), "0 */15 * * * ?"),
        new(typeof(HourlySweepJob), "0 0 * * * ?"),
        new(typeof(SixHourSweepJob), "0 0 */6 * * ?"),
        new(typeof(TwiceDailySweepJob), "0 0 6,18 * * ?"),
        new(typeof(NightlyMaintenanceJob), "0 0 3 * * ?"),

        // Channel availability pulls stay standalone: every 3 h is an
        // external-API budget, deliberately offset from the other channel
        // syncs in the composites.
        new(typeof(ChannelAvailabilitySyncJob), "0 0 */3 * * ?"),

        // Weekly truth-snapshot tamper verification (rehashes all confirmed
        // snapshots — too heavy to fold into a nightly sweep).
        new(typeof(SnapshotVerificationJob), "0 0 3 ? * SUN"),
    ];

    /// <summary>
    /// Job names that are allowed to exist in the persistent store. Anything
    /// else is a leftover from an older deployment (e.g. the removed module
    /// self-registrations, or tasks that were folded into composite sweeps)
    /// and is purged at startup by QuartzBootstrapService.
    /// </summary>
    public static IReadOnlySet<string> ExpectedJobNames { get; } =
        Jobs.Select(j => j.Name).ToHashSet(StringComparer.Ordinal);

    public static void RegisterAllJobs(IServiceCollectionQuartzConfigurator q)
    {
        ArgumentNullException.ThrowIfNull(q);

        foreach (var job in Jobs)
        {
            q.AddJob(job.JobType, new JobKey(job.Name));
            q.AddTrigger(t => t
                .ForJob(job.Name)
                .WithIdentity($"{job.Name}-trigger")
                .WithCronSchedule(job.CronExpression));
        }
    }
}
