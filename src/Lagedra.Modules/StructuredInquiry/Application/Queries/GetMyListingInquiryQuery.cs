using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Phase 17 — fetch the calling tenant's open pre-booking inquiry thread
/// for a specific listing, if one exists. Deal-linked threads belong to
/// the booking and are not returned here (listing CTA must start a new
/// thread for a new potential booking). Returns <c>Inquiry.NotFound</c>
/// when no open listing-scoped thread exists.
/// </summary>
public sealed record GetMyListingInquiryQuery(
    Guid ListingId,
    Guid TenantUserId) : IRequest<Result<InquiryDto>>;

public sealed class GetMyListingInquiryQueryHandler(InquiryDbContext dbContext)
    : IRequestHandler<GetMyListingInquiryQuery, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        GetMyListingInquiryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .Where(s => s.ListingId == request.ListingId
                && s.TenantUserId == request.TenantUserId
                && s.DealId == null
                && s.Status == InquirySessionStatus.Open)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No inquiry thread found for this listing."));
        }

        return Result<InquiryDto>.Success(InquiryDtoMapper.ToDto(session));
    }
}
