using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Phase 17 — fetch the calling tenant's pre-booking inquiry thread for a
/// specific listing, if one exists. Returns <c>Inquiry.NotFound</c> when
/// the tenant has not yet started a thread (the listing detail page uses
/// this to decide between "Ask a question" and "Continue conversation").
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

        // Most-recent thread first — under steady state there is exactly
        // one open session per (listing, tenant), but if the tenant
        // previously closed and re-opened we still want the latest one.
        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Where(s => s.ListingId == request.ListingId
                && s.TenantUserId == request.TenantUserId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No inquiry thread found for this listing."));
        }

        return Result<InquiryDto>.Success(MapToDto(session));
    }

    private static InquiryDto MapToDto(InquirySession s) =>
        new(s.Id, s.DealId, s.ListingId, s.TenantUserId, s.Status,
            s.UnlockedByLandlordAt, s.ClosedAt, s.CreatedAt,
            s.Questions.Select(q => new InquiryQuestionDto(
                q.Id,
                q.PredefinedQuestionId,
                q.Category,
                q.SubmittedAt,
                q.Answer is not null
                    ? new InquiryAnswerDto(q.Answer.Id, q.Answer.ResponseType,
                        q.Answer.AnswerValue, q.Answer.AnsweredAt)
                    : null,
                q.CustomText,
                q.OpenQuestionText))
            .ToList());
}
