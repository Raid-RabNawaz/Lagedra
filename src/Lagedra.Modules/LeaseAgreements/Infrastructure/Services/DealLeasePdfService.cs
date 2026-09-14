using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed partial class DealLeasePdfService(
    IMediator mediator,
    IDealLeaseDocumentStore documentStore,
    ILogger<DealLeasePdfService> logger) : IDealLeasePdfService
{
    public async Task<DealLeaseDocument?> GetOrGenerateAsync(Guid dealId, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GenerateDealLeasePdfCommand(dealId), ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Value.Content is { Length: > 0 })
        {
            LogGeneratedOnDemand(logger, dealId);
            return result.Value;
        }

        if (result.IsFailure)
        {
            LogGenerateFailed(logger, dealId, result.Error.Code, result.Error.Description);
        }

        // Same fallback the download endpoint uses: a previously sealed PDF
        // is still a valid agreement even when re-render fails.
        var existing = await documentStore.GetByDealIdAsync(dealId, ct).ConfigureAwait(false);
        if (existing is not null && existing.Content is { Length: > 0 })
        {
            LogFellBackToStored(logger, dealId);
            return existing;
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease PDF for deal {DealId} generated on demand")]
    private static partial void LogGeneratedOnDemand(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "On-demand lease PDF generation failed for deal {DealId}: {ErrorCode} {ErrorDescription}")]
    private static partial void LogGenerateFailed(ILogger logger, Guid dealId, string errorCode, string errorDescription);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease PDF for deal {DealId} served from the stored document after generation failed")]
    private static partial void LogFellBackToStored(ILogger logger, Guid dealId);
}
