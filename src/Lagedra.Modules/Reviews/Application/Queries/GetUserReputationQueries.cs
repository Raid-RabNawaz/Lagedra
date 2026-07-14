using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Queries;

public sealed record GetUserReviewsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<StayReviewDto>>>;

public sealed class GetUserReviewsQueryHandler(ReviewsDbContext dbContext)
    : IRequestHandler<GetUserReviewsQuery, Result<IReadOnlyList<StayReviewDto>>>
{
    public async Task<Result<IReadOnlyList<StayReviewDto>>> Handle(
        GetUserReviewsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviews = await dbContext.StayReviews
            .AsNoTracking()
            .Where(r => r.RevieweeUserId == request.UserId
                        && r.Status == StayReviewStatus.Published)
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<StayReviewDto>>.Success(
            reviews.Select(r => ReviewMapper.ToDto(r)).ToList());
    }
}

public sealed record GetUserReputationQuery(Guid UserId)
    : IRequest<Result<UserReputationDto>>;

public sealed class GetUserReputationQueryHandler(ReviewsDbContext dbContext)
    : IRequestHandler<GetUserReputationQuery, Result<UserReputationDto>>
{
    public async Task<Result<UserReputationDto>> Handle(
        GetUserReputationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviews = await dbContext.StayReviews
            .AsNoTracking()
            .Where(r => r.RevieweeUserId == request.UserId
                        && r.Status == StayReviewStatus.Published)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (reviews.Count == 0)
        {
            return Result<UserReputationDto>.Success(
                new UserReputationDto(request.UserId, 0, 0, new Dictionary<string, double>()));
        }

        var categories = new Dictionary<string, double>(StringComparer.Ordinal);
        void Avg(string key, Func<Domain.Aggregates.StayReview, int?> selector)
        {
            var vals = reviews.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (vals.Count > 0)
            {
                categories[key] = Math.Round(vals.Average(), 2);
            }
        }

        Avg("cleanliness", r => r.Cleanliness);
        Avg("accuracy", r => r.Accuracy);
        Avg("communication", r => r.Communication);
        Avg("location", r => r.Location);
        Avg("checkIn", r => r.CheckIn);
        Avg("value", r => r.Value);
        Avg("respectHouseRules", r => r.RespectHouseRules);

        return Result<UserReputationDto>.Success(new UserReputationDto(
            request.UserId,
            Math.Round(reviews.Average(r => r.OverallRating), 2),
            reviews.Count,
            categories));
    }
}

public sealed record GetListingReviewsQuery(Guid ListingId)
    : IRequest<Result<IReadOnlyList<StayReviewDto>>>;

public sealed class GetListingReviewsQueryHandler(ReviewsDbContext dbContext)
    : IRequestHandler<GetListingReviewsQuery, Result<IReadOnlyList<StayReviewDto>>>
{
    public async Task<Result<IReadOnlyList<StayReviewDto>>> Handle(
        GetListingReviewsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reviews = await dbContext.StayReviews
            .AsNoTracking()
            .Where(r => r.ListingId == request.ListingId
                        && r.Direction == StayReviewDirection.GuestToHost
                        && r.Status == StayReviewStatus.Published)
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<StayReviewDto>>.Success(
            reviews.Select(r => ReviewMapper.ToDto(r)).ToList());
    }
}
