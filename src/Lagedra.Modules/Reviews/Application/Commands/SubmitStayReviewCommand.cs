using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Commands;

public sealed record SubmitStayReviewCommand(
    Guid DealId,
    Guid CallerUserId,
    int OverallRating,
    string PublicComment,
    string? PrivateFeedback,
    int? Cleanliness,
    int? Accuracy,
    int? Communication,
    int? Location,
    int? CheckIn,
    int? Value,
    int? RespectHouseRules) : IRequest<Result<StayReviewDto>>;

public sealed class SubmitStayReviewCommandHandler(
    ReviewsDbContext dbContext,
    IDealApplicationStatusProvider dealProvider,
    IClock clock)
    : IRequestHandler<SubmitStayReviewCommand, Result<StayReviewDto>>
{
    public async Task<Result<StayReviewDto>> Handle(
        SubmitStayReviewCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var window = await dbContext.StayReviewWindows
            .FirstOrDefaultAsync(w => w.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (window is null)
        {
            return Result<StayReviewDto>.Failure(new Error(
                "Reviews.WindowNotOpen",
                "The review window for this stay has not opened yet."));
        }

        if (!window.IsOpen(clock))
        {
            return Result<StayReviewDto>.Failure(new Error(
                "Reviews.WindowClosed",
                "The review window for this stay has closed."));
        }

        StayReviewDirection direction;
        Guid revieweeId;
        if (request.CallerUserId == window.TenantUserId)
        {
            direction = StayReviewDirection.GuestToHost;
            revieweeId = window.LandlordUserId;
            if (window.GuestSubmitted)
            {
                return Result<StayReviewDto>.Failure(new Error(
                    "Reviews.AlreadySubmitted",
                    "You have already submitted a review for this stay."));
            }
        }
        else if (request.CallerUserId == window.LandlordUserId)
        {
            direction = StayReviewDirection.HostToGuest;
            revieweeId = window.TenantUserId;
            if (window.HostSubmitted)
            {
                return Result<StayReviewDto>.Failure(new Error(
                    "Reviews.AlreadySubmitted",
                    "You have already submitted a review for this stay."));
            }
        }
        else
        {
            return Result<StayReviewDto>.Failure(new Error(
                "Reviews.Forbidden",
                "Only the host or guest on this stay can leave a review."));
        }

        // Confirm deal still matches (defense in depth).
        var participants = await dealProvider
            .GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return Result<StayReviewDto>.Failure(new Error(
                "Reviews.DealNotFound", "Deal not found."));
        }

        StayReview review;
        try
        {
            review = StayReview.Submit(
                request.DealId,
                window.ListingId,
                request.CallerUserId,
                revieweeId,
                direction,
                request.OverallRating,
                request.PublicComment,
                request.PrivateFeedback,
                new StayReviewCategories
                {
                    Cleanliness = request.Cleanliness,
                    Accuracy = request.Accuracy,
                    Communication = request.Communication,
                    Location = request.Location,
                    CheckIn = request.CheckIn,
                    Value = request.Value,
                    RespectHouseRules = request.RespectHouseRules
                },
                clock);
        }
        catch (ArgumentException ex)
        {
            return Result<StayReviewDto>.Failure(new Error("Reviews.Invalid", ex.Message));
        }

        dbContext.StayReviews.Add(review);

        if (direction == StayReviewDirection.GuestToHost)
        {
            window.MarkGuestSubmitted(clock);
        }
        else
        {
            window.MarkHostSubmitted(clock);
        }

        if (window.ShouldPublish(clock))
        {
            await PublishWindowAsync(window, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<StayReviewDto>.Success(ReviewMapper.ToDto(review));
    }

    private async Task PublishWindowAsync(StayReviewWindow window, CancellationToken ct)
    {
        var reviews = await dbContext.StayReviews
            .Where(r => r.DealId == window.DealId && r.Status == StayReviewStatus.Submitted)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var review in reviews)
        {
            review.Publish(clock);
        }

        window.MarkPublished(clock);
    }
}
