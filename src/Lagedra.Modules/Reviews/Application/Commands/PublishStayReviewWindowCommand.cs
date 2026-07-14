using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Commands;

public sealed record PublishStayReviewWindowCommand(Guid DealId) : IRequest<Result>;

public sealed class PublishStayReviewWindowCommandHandler(
    ReviewsDbContext dbContext,
    IClock clock)
    : IRequestHandler<PublishStayReviewWindowCommand, Result>
{
    public async Task<Result> Handle(PublishStayReviewWindowCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var window = await dbContext.StayReviewWindows
            .FirstOrDefaultAsync(w => w.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (window is null)
        {
            return Result.Failure(new Error("Reviews.WindowNotFound", "Review window not found."));
        }

        if (window.IsPublished || !window.ShouldPublish(clock))
        {
            return Result.Success();
        }

        var reviews = await dbContext.StayReviews
            .Where(r => r.DealId == window.DealId && r.Status == StayReviewStatus.Submitted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var review in reviews)
        {
            review.Publish(clock);
        }

        window.MarkPublished(clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
