using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class ComplianceScannerJob(
    ComplianceMonitoringDbContext dbContext,
    ILogger<ComplianceScannerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationToken = context.CancellationToken;

        await ScanForInsuranceLapsesAsync(cancellationToken).ConfigureAwait(false);
        await ScanForOverdueCureWindowsAsync(cancellationToken).ConfigureAwait(false);

        LogScanComplete(logger);
    }

    private async Task ScanForInsuranceLapsesAsync(CancellationToken cancellationToken)
    {
        var dealsWithSignals = await dbContext.Signals
            .AsNoTracking()
            .Where(s => s.SignalType == "InsuranceLapse" && s.ProcessedAt == null)
            .Select(s => s.DealId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var dealId in dealsWithSignals)
        {
            var existingViolation = await dbContext.Violations
                .AnyAsync(
                    v => v.DealId == dealId
                         && v.Category == MonitoredViolationCategory.CategoryA
                         && v.Status == MonitoredViolationStatus.Open,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingViolation)
            {
                continue;
            }

            var violation = MonitoredViolation.Create(
                dealId,
                MonitoredViolationCategory.CategoryA,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30));

            dbContext.Violations.Add(violation);
            LogInsuranceLapseDetected(logger, dealId, violation.Id);
        }

        var unprocessedSignals = await dbContext.Signals
            .Where(s => s.SignalType == "InsuranceLapse" && s.ProcessedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var signal in unprocessedSignals)
        {
            signal.MarkProcessed(DateTime.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ScanForOverdueCureWindowsAsync(CancellationToken cancellationToken)
    {
        var overdueViolations = await dbContext.Violations
            .Where(v => v.Status == MonitoredViolationStatus.Open
                        && v.CureDeadline != null
                        && v.CureDeadline < DateTime.UtcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var violation in overdueViolations)
        {
            violation.Escalate();
            LogCureWindowExpired(logger, violation.Id, violation.DealId);
        }

        if (overdueViolations.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        LogOverdueScanComplete(logger, overdueViolations.Count);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insurance lapse detected for deal {DealId}, violation {ViolationId} created")]
    private static partial void LogInsuranceLapseDetected(ILogger logger, Guid dealId, Guid violationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cure window expired for violation {ViolationId} (Deal {DealId}), escalating")]
    private static partial void LogCureWindowExpired(ILogger logger, Guid violationId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Overdue cure window scan complete: {EscalatedCount} violations escalated")]
    private static partial void LogOverdueScanComplete(ILogger logger, int escalatedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Compliance scan cycle complete")]
    private static partial void LogScanComplete(ILogger logger);
}
