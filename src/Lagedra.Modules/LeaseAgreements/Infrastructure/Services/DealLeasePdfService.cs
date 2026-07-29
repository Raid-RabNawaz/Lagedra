using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed partial class DealLeasePdfService(
    IDealLeaseDocumentStore store,
    IMediator mediator,
    ILogger<DealLeasePdfService> logger) : IDealLeasePdfService
{
    public async Task<DealLeaseDocument?> GetOrGenerateAsync(Guid dealId, CancellationToken ct = default)
    {
        var existing = await store.GetByDealIdAsync(dealId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var result = await mediator.Send(new GenerateDealLeasePdfCommand(dealId), ct)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            LogGeneratedOnDemand(logger, dealId);
            return result.Value;
        }

        LogGenerateFailed(logger, dealId, result.Error.Code, result.Error.Description);
        return null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Lease PDF for deal {DealId} generated on demand")]
    private static partial void LogGeneratedOnDemand(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "On-demand lease PDF generation failed for deal {DealId}: {ErrorCode} {ErrorDescription}")]
    private static partial void LogGenerateFailed(ILogger logger, Guid dealId, string errorCode, string errorDescription);
}
