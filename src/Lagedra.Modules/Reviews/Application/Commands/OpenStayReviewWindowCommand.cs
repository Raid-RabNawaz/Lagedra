using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Application.Commands;

public sealed record OpenStayReviewWindowCommand(Guid DealId) : IRequest<Result>;

public sealed class OpenStayReviewWindowCommandHandler(
    ReviewsDbContext dbContext,
    IDealApplicationStatusProvider dealProvider,
    IPlatformSettingsService settings,
    IClock clock)
    : IRequestHandler<OpenStayReviewWindowCommand, Result>
{
    public async Task<Result> Handle(OpenStayReviewWindowCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exists = await dbContext.StayReviewWindows
            .AnyAsync(w => w.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return Result.Success();
        }

        var participants = await dealProvider
            .GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return Result.Failure(new Error(
                "Reviews.DealNotFound",
                "Deal participants could not be resolved for the review window."));
        }

        var windowDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.ReviewWindowDays, 14, cancellationToken)
            .ConfigureAwait(false);

        var window = StayReviewWindow.Open(
            request.DealId,
            participants.ListingId,
            participants.LandlordUserId,
            participants.TenantUserId,
            windowDays,
            clock);

        dbContext.StayReviewWindows.Add(window);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

/// <summary>Bridges StayCompletedEvent → OpenStayReviewWindowCommand.</summary>
public sealed class OnStayCompletedOpenReviewWindowHandler(IMediator mediator)
    : IDomainEventHandler<StayCompletedEvent>
{
    public async Task Handle(StayCompletedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await mediator.Send(new OpenStayReviewWindowCommand(domainEvent.DealId), ct)
            .ConfigureAwait(false);
    }
}
