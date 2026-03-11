using System.Collections.Concurrent;
using System.Net;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;
using Lagedra.Modules.Evidence.Infrastructure.Jobs;
using Lagedra.SharedKernel.Email;
using Quartz;
using Quartz.Impl.Matchers;

namespace Lagedra.Worker.Orchestration;

internal sealed partial class HealthOrchestrator(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<HealthOrchestrator> logger) : BackgroundService, IJobListener
{
    private readonly ConcurrentDictionary<string, int> _consecutiveFailures = new();
    private const int FailureThreshold = 3;

    private static readonly HashSet<string> CriticalJobs =
    [
        nameof(MalwareScanPollingJob),
        nameof(PaymentConfirmationTimeoutJob)
    ];

    public string Name => nameof(HealthOrchestrator);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        var schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
        var scheduler = await schedulerFactory.GetScheduler(stoppingToken).ConfigureAwait(false);
        scheduler.ListenerManager.AddJobListener(this, GroupMatcher<JobKey>.AnyGroup());

        LogStarted(logger);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobName = context.JobDetail.Key.Name;

        if (jobException is null)
        {
            _consecutiveFailures.TryRemove(jobName, out _);
            return;
        }

        var count = _consecutiveFailures.AddOrUpdate(jobName, 1, (_, c) => c + 1);
        LogJobFailed(logger, jobName, count);

        if (CriticalJobs.Contains(jobName) && count >= FailureThreshold)
        {
            await SendAlertAsync(jobName, count, jobException, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendAlertAsync(string jobName, int failureCount, Exception ex, CancellationToken ct)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var alertEmail = configuration["Worker:AlertEmail"] ?? "ops@lagedra.com";
            var encodedMessage = WebUtility.HtmlEncode(ex.Message);

            await emailService.SendAsync(new EmailMessage
            {
                To = alertEmail,
                Subject = $"[Lagedra Worker] Critical job '{jobName}' failed {failureCount} times",
                HtmlBody = $"<p>The critical job <strong>{jobName}</strong> has failed " +
                           $"<strong>{failureCount}</strong> consecutive times.</p>" +
                           $"<p>Latest error: {encodedMessage}</p>"
            }, ct).ConfigureAwait(false);

            LogAlertSent(logger, jobName, alertEmail);
        }
#pragma warning disable CA1031 // Alert failure must not crash the health monitor
        catch (Exception alertEx)
#pragma warning restore CA1031
        {
            LogAlertFailed(logger, jobName, alertEx);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HealthOrchestrator started and listening for job events")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobName} failed, consecutive failure count: {FailureCount}")]
    private static partial void LogJobFailed(ILogger logger, string jobName, int failureCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Alert email sent for critical job {JobName} to {AlertEmail}")]
    private static partial void LogAlertSent(ILogger logger, string jobName, string alertEmail);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send alert email for job {JobName}")]
    private static partial void LogAlertFailed(ILogger logger, string jobName, Exception ex);
}
