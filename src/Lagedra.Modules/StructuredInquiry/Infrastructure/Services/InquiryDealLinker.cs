using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Services;

/// <summary>
/// Phase 17 — implementation of <see cref="IInquiryDealLinker"/> that the
/// ActivationAndBilling module calls right after a deal is created from
/// an application. Looks up the tenant's most recent open listing-scoped
/// inquiry session for the same listing and links it to the new deal so
/// the conversation thread is preserved on the resulting deal page.
/// </summary>
public sealed partial class InquiryDealLinker(
    InquiryDbContext dbContext,
    ILogger<InquiryDealLinker> logger) : IInquiryDealLinker
{
    public async Task LinkOpenInquiryToDealAsync(
        Guid listingId,
        Guid tenantUserId,
        Guid dealId,
        CancellationToken ct = default)
    {
        var session = await dbContext.Sessions
            .Where(s => s.ListingId == listingId
                && s.TenantUserId == tenantUserId
                && s.Status == InquirySessionStatus.Open
                && (s.DealId == null || s.DealId == dealId))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (session is null)
        {
            return;
        }

        if (session.DealId == dealId)
        {
            return;
        }

        try
        {
            session.LinkToDeal(dealId);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogLinked(logger, session.Id, dealId, listingId);
        }
        catch (InvalidOperationException ex)
        {
            LogLinkFailed(logger, ex, session.Id, dealId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Linked pre-booking inquiry session {SessionId} to deal {DealId} for listing {ListingId}.")]
    private static partial void LogLinked(
        ILogger logger, Guid sessionId, Guid dealId, Guid listingId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to link inquiry session {SessionId} to deal {DealId}.")]
    private static partial void LogLinkFailed(
        ILogger logger, Exception ex, Guid sessionId, Guid dealId);
}
