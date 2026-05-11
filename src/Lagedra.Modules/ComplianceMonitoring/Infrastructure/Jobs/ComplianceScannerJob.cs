using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class ComplianceScannerJob(
    ComplianceMonitoringDbContext dbContext,
    IDealApplicationStatusProvider dealProvider,
    IMediator mediator,
    ILogger<ComplianceScannerJob> logger) : IJob
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationToken = context.CancellationToken;

        await ScanForInsuranceLapsesAsync(cancellationToken).ConfigureAwait(false);
        await ScanForPaymentDefaultsAsync(cancellationToken).ConfigureAwait(false);
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

        var createdViolations = new List<(Guid DealId, Guid ViolationId)>();

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
            createdViolations.Add((dealId, violation.Id));
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

        foreach (var (dealId, violationId) in createdViolations)
        {
            await NotifyParticipantsAsync(
                dealId, "compliance_violation_detected",
                "Insurance Lapse Detected",
                "An insurance lapse has been detected on your deal. A 30-day cure window has been opened.",
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ScanForPaymentDefaultsAsync(CancellationToken cancellationToken)
    {
        var dealsWithSignals = await dbContext.Signals
            .AsNoTracking()
            .Where(s => s.SignalType == "PaymentDefault" && s.ProcessedAt == null)
            .Select(s => s.DealId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var createdViolations = new List<(Guid DealId, Guid ViolationId)>();

        foreach (var dealId in dealsWithSignals)
        {
            var existingViolation = await dbContext.Violations
                .AnyAsync(
                    v => v.DealId == dealId
                         && v.Category == MonitoredViolationCategory.CategoryB
                         && v.Status == MonitoredViolationStatus.Open,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingViolation)
            {
                continue;
            }

            var violation = MonitoredViolation.Create(
                dealId,
                MonitoredViolationCategory.CategoryB,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(14));

            dbContext.Violations.Add(violation);
            createdViolations.Add((dealId, violation.Id));
            LogPaymentDefaultDetected(logger, dealId, violation.Id);
        }

        var unprocessedSignals = await dbContext.Signals
            .Where(s => s.SignalType == "PaymentDefault" && s.ProcessedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var signal in unprocessedSignals)
        {
            signal.MarkProcessed(DateTime.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (dealId, violationId) in createdViolations)
        {
            await NotifyParticipantsAsync(
                dealId, "compliance_violation_detected",
                "Payment Default Detected",
                "A payment default has been detected on your deal. A 14-day cure window has been opened.",
                cancellationToken).ConfigureAwait(false);
        }
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

            var escalatedDealIds = overdueViolations.Select(v => v.DealId).Distinct();
            foreach (var dealId in escalatedDealIds)
            {
                await NotifyParticipantsAsync(
                    dealId, "compliance_violation_escalated",
                    "Compliance Violation Escalated",
                    "A compliance violation on your deal has been escalated because the cure window expired.",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        LogOverdueScanComplete(logger, overdueViolations.Count);
    }

    private async Task NotifyParticipantsAsync(
        Guid dealId, string templateId, string title, string body, CancellationToken ct)
    {
        var participants = await dealProvider
            .GetParticipantsAsync(dealId, ct)
            .ConfigureAwait(false);

        if (participants is null) return;

        var data = new Dictionary<string, string> { ["dealId"] = dealId.ToString() };

        await mediator.Send(new NotifyUserCommand(
            participants.LandlordUserId, templateId, title, body,
            data, EmailAndInApp, dealId, "Deal"), ct).ConfigureAwait(false);

        await mediator.Send(new NotifyUserCommand(
            participants.TenantUserId, templateId, title, body,
            data, EmailAndInApp, dealId, "Deal"), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insurance lapse detected for deal {DealId}, violation {ViolationId} created")]
    private static partial void LogInsuranceLapseDetected(ILogger logger, Guid dealId, Guid violationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment default detected for deal {DealId}, violation {ViolationId} created")]
    private static partial void LogPaymentDefaultDetected(ILogger logger, Guid dealId, Guid violationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cure window expired for violation {ViolationId} (Deal {DealId}), escalating")]
    private static partial void LogCureWindowExpired(ILogger logger, Guid violationId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Overdue cure window scan complete: {EscalatedCount} violations escalated")]
    private static partial void LogOverdueScanComplete(ILogger logger, int escalatedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Compliance scan cycle complete")]
    private static partial void LogScanComplete(ILogger logger);
}
