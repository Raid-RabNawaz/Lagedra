using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Queries;

public sealed record ListPartnerServiceReviewsQuery(Guid OrganizationId)
    : IRequest<Result<IReadOnlyList<PartnerServiceReviewDto>>>;

public sealed class ListPartnerServiceReviewsQueryHandler(ReviewsDbContext dbContext)
    : IRequestHandler<ListPartnerServiceReviewsQuery, Result<IReadOnlyList<PartnerServiceReviewDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerServiceReviewDto>>> Handle(
        ListPartnerServiceReviewsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviews = await dbContext.PartnerServiceReviews
            .AsNoTracking()
            .Where(r => r.OrganizationId == request.OrganizationId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PartnerServiceReviewDto>>.Success(
            reviews.Select(ReviewMapper.ToDto).ToList());
    }
}

public sealed record GetPartnerReputationQuery(Guid OrganizationId, Guid? CallerUserId)
    : IRequest<Result<PartnerReputationDto>>;

public sealed class GetPartnerReputationQueryHandler(
    ReviewsDbContext dbContext,
    IPartnerEndorsementProvider endorsementProvider)
    : IRequestHandler<GetPartnerReputationQuery, Result<PartnerReputationDto>>
{
    public async Task<Result<PartnerReputationDto>> Handle(
        GetPartnerReputationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviews = await dbContext.PartnerServiceReviews
            .AsNoTracking()
            .Where(r => r.OrganizationId == request.OrganizationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var alreadyReviewed = false;
        var canReview = false;
        if (request.CallerUserId is Guid callerId)
        {
            alreadyReviewed = reviews.Any(r => r.ReviewerUserId == callerId);
            if (!alreadyReviewed)
            {
                var endorsementId = await endorsementProvider
                    .GetReviewEligibleEndorsementIdAsync(callerId, request.OrganizationId, cancellationToken)
                    .ConfigureAwait(false);
                canReview = endorsementId is not null;
            }
        }

        if (reviews.Count == 0)
        {
            return Result<PartnerReputationDto>.Success(new PartnerReputationDto(
                request.OrganizationId, 0, 0, 0, 0, 0, canReview, alreadyReviewed));
        }

        return Result<PartnerReputationDto>.Success(new PartnerReputationDto(
            request.OrganizationId,
            Math.Round(reviews.Average(r => r.OverallRating), 2),
            reviews.Count,
            Math.Round(reviews.Average(r => r.Responsiveness), 2),
            Math.Round(reviews.Average(r => r.Reliability), 2),
            Math.Round(reviews.Average(r => r.SupportQuality), 2),
            canReview,
            alreadyReviewed));
    }
}
