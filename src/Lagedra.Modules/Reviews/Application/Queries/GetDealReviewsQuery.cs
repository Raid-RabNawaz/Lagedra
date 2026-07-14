using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Queries;

public sealed record GetDealReviewsQuery(Guid DealId, Guid CallerUserId, bool IsAdmin = false)
    : IRequest<Result<StayReviewWindowDto>>;

public sealed class GetDealReviewsQueryHandler(
    ReviewsDbContext dbContext,
    IClock clock)
    : IRequestHandler<GetDealReviewsQuery, Result<StayReviewWindowDto>>
{
    public async Task<Result<StayReviewWindowDto>> Handle(
        GetDealReviewsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var window = await dbContext.StayReviewWindows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (window is null)
        {
            return Result<StayReviewWindowDto>.Failure(new Error(
                "Reviews.WindowNotFound",
                "No review window exists for this deal yet."));
        }

        var reviews = await dbContext.StayReviews
            .AsNoTracking()
            .Where(r => r.DealId == request.DealId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        StayReviewDirection? callerDirection = null;
        var canSubmit = false;
        if (request.CallerUserId == window.TenantUserId)
        {
            callerDirection = StayReviewDirection.GuestToHost;
            canSubmit = window.IsOpen(clock) && !window.GuestSubmitted;
        }
        else if (request.CallerUserId == window.LandlordUserId)
        {
            callerDirection = StayReviewDirection.HostToGuest;
            canSubmit = window.IsOpen(clock) && !window.HostSubmitted;
        }

        var own = reviews.FirstOrDefault(r => r.ReviewerUserId == request.CallerUserId);
        var peer = reviews.FirstOrDefault(r => r.ReviewerUserId != request.CallerUserId);

        StayReviewDto? peerDto = null;
        if (peer is not null && (window.IsPublished || request.IsAdmin))
        {
            peerDto = ReviewMapper.ToDto(peer);
        }

        return Result<StayReviewWindowDto>.Success(new StayReviewWindowDto(
            window.DealId,
            window.ListingId,
            window.OpensAt,
            window.ClosesAt,
            window.GuestSubmitted,
            window.HostSubmitted,
            window.IsPublished,
            window.PublishedAt,
            canSubmit,
            callerDirection,
            own is null ? null : ReviewMapper.ToDto(own),
            peerDto));
    }
}
