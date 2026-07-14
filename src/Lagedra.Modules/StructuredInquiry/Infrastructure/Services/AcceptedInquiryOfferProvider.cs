using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Services;

/// <summary>
/// Resolves the accepted rent/deposit offer for a listing + tenant so
/// reservation pricing can snapshot negotiated terms at Apply.
/// </summary>
public sealed class AcceptedInquiryOfferProvider(InquiryDbContext dbContext)
    : IAcceptedInquiryOfferProvider
{
    public async Task<AcceptedInquiryOfferDto?> GetAcceptedOfferAsync(
        Guid listingId,
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        // Prefer the most recent session that still has an Accepted offer.
        // Closed sessions keep the accepted offer for audit, but pricing only
        // applies while the tenant can still apply — i.e. session is Open or
        // already linked to a deal that hasn't sealed yet. We still return
        // Accepted on Open/Closed/Locked so a mid-apply race is covered; the
        // submit path is the consumer.
        var offer = await dbContext.Offers
            .AsNoTracking()
            .Where(o => o.Status == InquiryOfferStatus.Accepted)
            .Join(
                dbContext.Sessions.AsNoTracking()
                    .Where(s => s.ListingId == listingId && s.TenantUserId == tenantUserId),
                o => o.SessionId,
                s => s.Id,
                (o, s) => new { Offer = o, Session = s })
            .OrderByDescending(x => x.Offer.RespondedAt ?? x.Offer.ProposedAt)
            .Select(x => new AcceptedInquiryOfferDto(
                x.Offer.Id,
                x.Offer.SessionId,
                x.Offer.RentCents,
                x.Offer.DepositCents))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return offer;
    }
}
