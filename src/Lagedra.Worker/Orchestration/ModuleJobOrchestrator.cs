using System.Collections.Concurrent;

namespace Lagedra.Worker.Orchestration;

internal sealed partial class ModuleJobOrchestrator(ILogger<ModuleJobOrchestrator> logger)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _runningJobs = new();

    public bool TryStartJob(string jobName)
    {
        ArgumentNullException.ThrowIfNull(jobName);

        if (_runningJobs.TryAdd(jobName, DateTimeOffset.UtcNow))
        {
            LogJobStarted(logger, jobName);
            return true;
        }

        LogJobAlreadyRunning(logger, jobName);
        return false;
    }

    public void CompleteJob(string jobName)
    {
        ArgumentNullException.ThrowIfNull(jobName);

        _runningJobs.TryRemove(jobName, out _);
        LogJobCompleted(logger, jobName);
    }

    public IReadOnlyDictionary<string, DateTimeOffset> RunningJobs => _runningJobs;

    [LoggerMessage(Level = LogLevel.Information, Message = "Module job {JobName} started")]
    private static partial void LogJobStarted(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Module job {JobName} already running — skipped")]
    private static partial void LogJobAlreadyRunning(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Module job {JobName} completed")]
    private static partial void LogJobCompleted(ILogger logger, string jobName);
}
