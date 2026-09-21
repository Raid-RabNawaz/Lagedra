using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.LeaseAgreements.Application.EventHandlers;

public sealed partial class OnTruthSurfaceConfirmedGenerateLeasePdfHandler(
    IMediator mediator,
    ILogger<OnTruthSurfaceConfirmedGenerateLeasePdfHandler> logger)
    : IDomainEventHandler<TruthSurfaceConfirmedEvent>
{
    public async Task Handle(TruthSurfaceConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var result = await mediator.Send(
            new GenerateDealLeasePdfCommand(domainEvent.DealId, domainEvent.SnapshotId), ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(result.Error.Code, "LeaseAgreement.NotUsed", StringComparison.Ordinal))
            {
                LogPdfSkipped(logger, domainEvent.DealId);
            }
            else
            {
                LogPdfFailed(logger, domainEvent.DealId, result.Error.Description);
            }
        }
        else
        {
            LogPdfGenerated(logger, domainEvent.DealId, result.Value.FileName);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Lease PDF generation failed for deal {DealId}: {Error}")]
    private static partial void LogPdfFailed(ILogger logger, Guid dealId, string? error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped lease PDF for deal {DealId} — listing does not use a lease agreement")]
    private static partial void LogPdfSkipped(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease PDF generated for deal {DealId}: {FileName}")]
    private static partial void LogPdfGenerated(ILogger logger, Guid dealId, string fileName);
}
