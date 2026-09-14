using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using ScreeningPort = Lagedra.SharedKernel.Integration.Screening;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record RefundIncompleteScreeningOrdersCommand : IRequest<Result<int>>;

public sealed class RefundIncompleteScreeningOrdersCommandHandler(
    IdentityDbContext dbContext,
    ScreeningPort.ITenantScreeningProvider screeningProvider,
    IStripeService stripeService,
    IListingProvider listingProvider,
    IMediator mediator,
    IOptions<ScreeningOptions> screeningOptions,
    IClock clock)
    : IRequestHandler<RefundIncompleteScreeningOrdersCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        RefundIncompleteScreeningOrdersCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var days = Math.Max(1, screeningOptions.Value.IncompleteApplyRefundAfterDays);
        var cutoff = clock.UtcNow.AddDays(-days);

        var stale = await dbContext.ScreeningOrders
            .Where(o => o.Status == ScreeningOrderStatus.WaitingForApplicant
                        && (o.PaidAt ?? o.CreatedAt) <= cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var refunded = 0;
        foreach (var order in stale)
        {
            if (!string.IsNullOrWhiteSpace(order.ExternalOrderId))
            {
                await screeningProvider
                    .CancelOrderAsync(order.ExternalOrderId, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            {
                await stripeService.RefundPaymentIntentAsync(
                    order.StripePaymentIntentId,
                    amountCents: null,
                    reverseTransfer: false,
                    refundApplicationFee: false,
                    idempotencyKey: $"screening-incomplete-{order.Id:N}",
                    cancellationToken).ConfigureAwait(false);
            }

            order.MarkCanceled();
            refunded++;

            var listing = await listingProvider
                .GetListingDetailsAsync(order.ListingId, cancellationToken)
                .ConfigureAwait(false);

            await mediator.Send(
                new NotifyUserCommand(
                    order.HostUserId,
                    "screening_incomplete_refunded",
                    "Screening refunded",
                    "The guest did not complete the screening, so the check fee was refunded.",
                    new Dictionary<string, string>
                    {
                        ["listingTitle"] = listing?.Title ?? "your listing",
                        ["applicationId"] = order.ApplicationId.ToString()
                    },
                    [NotificationChannel.Email, NotificationChannel.InApp],
                    order.ApplicationId,
                    "DealApplication"),
                cancellationToken).ConfigureAwait(false);
        }

        if (refunded > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<int>.Success(refunded);
    }
}
