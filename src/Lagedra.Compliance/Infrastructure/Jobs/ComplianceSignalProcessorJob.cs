using Lagedra.Compliance.Application.Commands;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Compliance.Infrastructure.Jobs;

/// <summary>
/// Processes unprocessed ComplianceSignal records. Converts signals into
/// violations or trust ledger entries based on signal type.
/// Runs every 15 minutes.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ComplianceSignalProcessorJob(
    ComplianceDbContext dbContext,
    IDealApplicationStatusProvider dealProvider,
    IMediator mediator,
    ILogger<ComplianceSignalProcessorJob> logger) : IJob
{
    private const int BatchSize = 50;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationToken = context.CancellationToken;

        var unprocessed = await dbContext.Signals
            .Where(s => !s.Processed)
            .OrderBy(s => s.ReceivedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (unprocessed.Count == 0)
        {
            LogNoSignals(logger);
            return;
        }

        LogProcessingSignals(logger, unprocessed.Count);

        foreach (var signal in unprocessed)
        {
            try
            {
                await ProcessSignalAsync(signal.DealId, signal.SignalType, signal.Payload, cancellationToken)
                    .ConfigureAwait(false);

                signal.MarkProcessed();
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogSignalProcessingFailed(logger, signal.Id, signal.SignalType, ex);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogProcessingComplete(logger, unprocessed.Count);
    }

    private async Task ProcessSignalAsync(
        Guid dealId,
        string signalType,
        string? payload,
        CancellationToken ct)
    {
        var participants = await dealProvider
            .GetParticipantsAsync(dealId, ct)
            .ConfigureAwait(false);

        var landlordId = participants?.LandlordUserId ?? Guid.Empty;
        var tenantId = participants?.TenantUserId ?? Guid.Empty;

        switch (signalType)
        {
            case "InsuranceLapse":
                var insuranceViolation = await mediator.Send(new RecordViolationCommand(
                    dealId, landlordId, tenantId,
                    Domain.ViolationCategory.InsuranceLapse,
                    payload ?? "Insurance lapse detected via compliance signal",
                    null), ct)
                    .ConfigureAwait(false);

                if (insuranceViolation.IsSuccess)
                {
                    await mediator.Send(new RecordLedgerEntryCommand(
                        tenantId,
                        Domain.TrustLedgerEntryType.ViolationRecorded,
                        insuranceViolation.Value.Id,
                        "Insurance lapse violation recorded for deal",
                        false), ct)
                        .ConfigureAwait(false);
                }

                break;

            case "PaymentDefault":
                var paymentViolation = await mediator.Send(new RecordViolationCommand(
                    dealId, landlordId, tenantId,
                    Domain.ViolationCategory.NonPayment,
                    payload ?? "Payment default detected via compliance signal",
                    null), ct)
                    .ConfigureAwait(false);

                if (paymentViolation.IsSuccess)
                {
                    await mediator.Send(new RecordLedgerEntryCommand(
                        tenantId,
                        Domain.TrustLedgerEntryType.PaymentDefault,
                        paymentViolation.Value.Id,
                        "Payment default violation recorded for deal",
                        false), ct)
                        .ConfigureAwait(false);
                }

                break;

            case "DealCompleted":
                await mediator.Send(new RecordLedgerEntryCommand(
                    tenantId,
                    Domain.TrustLedgerEntryType.DealCompleted,
                    dealId,
                    payload ?? "Deal completed successfully",
                    true), ct)
                    .ConfigureAwait(false);
                break;

            case "PositiveReview":
                await mediator.Send(new RecordLedgerEntryCommand(
                    tenantId,
                    Domain.TrustLedgerEntryType.PositiveReview,
                    dealId,
                    payload ?? "Positive review received",
                    true), ct)
                    .ConfigureAwait(false);
                break;

            default:
                await mediator.Send(new RecordLedgerEntryCommand(
                    tenantId,
                    Domain.TrustLedgerEntryType.ViolationRecorded,
                    dealId,
                    payload ?? $"Compliance signal: {signalType}",
                    false), ct)
                    .ConfigureAwait(false);
                break;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No unprocessed compliance signals")]
    private static partial void LogNoSignals(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} compliance signals")]
    private static partial void LogProcessingSignals(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process signal {SignalId} of type {SignalType}")]
    private static partial void LogSignalProcessingFailed(ILogger logger, Guid signalId, string signalType, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Compliance signal processing complete: {Count} signals processed")]
    private static partial void LogProcessingComplete(ILogger logger, int count);
}
