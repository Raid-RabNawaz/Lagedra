using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Commands;

public sealed record SubmitPartnerServiceReviewCommand(
    Guid OrganizationId,
    Guid ReviewerUserId,
    int OverallRating,
    int Responsiveness,
    int Reliability,
    int SupportQuality,
    string PublicComment) : IRequest<Result<PartnerServiceReviewDto>>;

public sealed class SubmitPartnerServiceReviewCommandHandler(
    ReviewsDbContext dbContext,
    IPartnerEndorsementProvider endorsementProvider,
    IClock clock)
    : IRequestHandler<SubmitPartnerServiceReviewCommand, Result<PartnerServiceReviewDto>>
{
    public async Task<Result<PartnerServiceReviewDto>> Handle(
        SubmitPartnerServiceReviewCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var already = await dbContext.PartnerServiceReviews
            .AnyAsync(
                r => r.OrganizationId == request.OrganizationId
                     && r.ReviewerUserId == request.ReviewerUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (already)
        {
            return Result<PartnerServiceReviewDto>.Failure(new Error(
                "Reviews.AlreadySubmitted",
                "You have already reviewed this partner."));
        }

        var endorsementId = await endorsementProvider
            .GetReviewEligibleEndorsementIdAsync(
                request.ReviewerUserId, request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (endorsementId is null)
        {
            return Result<PartnerServiceReviewDto>.Failure(new Error(
                "Reviews.NotEndorsed",
                "Only members endorsed by this partner can leave a service review."));
        }

        PartnerServiceReview review;
        try
        {
            review = PartnerServiceReview.Submit(
                request.OrganizationId,
                endorsementId.Value,
                request.ReviewerUserId,
                request.OverallRating,
                request.Responsiveness,
                request.Reliability,
                request.SupportQuality,
                request.PublicComment,
                clock);
        }
        catch (ArgumentException ex)
        {
            return Result<PartnerServiceReviewDto>.Failure(new Error("Reviews.Invalid", ex.Message));
        }

        dbContext.PartnerServiceReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerServiceReviewDto>.Success(ReviewMapper.ToDto(review));
    }
}
