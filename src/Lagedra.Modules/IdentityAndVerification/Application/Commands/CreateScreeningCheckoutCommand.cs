using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.Modules.IdentityAndVerification.Application.Services;
using Lagedra.SharedKernel.Settings;
using ScreeningPort = Lagedra.SharedKernel.Integration.Screening;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CreateScreeningCheckoutCommand(
    Guid ApplicationId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<Result<ScreeningCheckoutDto>>;

public sealed class CreateScreeningCheckoutCommandHandler(
    IdentityDbContext dbContext,
    IApplicationScreeningContextProvider applicationContext,
    IStripeService stripeService,
    ScreeningPort.ITenantScreeningProvider screeningProvider,
    IMediator mediator,
    IPlatformSettingsService platformSettings,
    IOptions<ScreeningOptions> screeningOptions)
    : IRequestHandler<CreateScreeningCheckoutCommand, Result<ScreeningCheckoutDto>>
{
    private const string Currency = "usd";
    private static readonly string[] PayableStatuses =
        ["requires_payment_method", "requires_confirmation", "requires_action"];

    public async Task<Result<ScreeningCheckoutDto>> Handle(
        CreateScreeningCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var app = await applicationContext
            .GetAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app is null)
        {
            return Result<ScreeningCheckoutDto>.Failure(
                new Error("Application.NotFound", "Application not found."));
        }

        if (!ScreeningAccess.CanRequest(app, request.RequestingUserId, request.IsAdmin))
        {
            return Result<ScreeningCheckoutDto>.Failure(
                new Error("Application.Forbidden", "Only the host can request a screening."));
        }

        if (!string.Equals(app.Status, "Pending", StringComparison.Ordinal))
        {
            return Result<ScreeningCheckoutDto>.Failure(
                new Error("Screening.NotPending", "Screening can only be requested on a pending application."));
        }

        var order = await dbContext.ScreeningOrders
            .Where(o => o.ApplicationId == request.ApplicationId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (order is { Status: ScreeningOrderStatus.WaitingForApplicant
            or ScreeningOrderStatus.Pending or ScreeningOrderStatus.Paid })
        {
            return Result<ScreeningCheckoutDto>.Failure(
                new Error("Screening.AlreadyOpen", "A screening is already in progress for this application."));
        }

        var reusePaidFailure = order is { Status: ScreeningOrderStatus.Failed }
            && !string.IsNullOrWhiteSpace(order.StripePaymentIntentId);
        if (order is null
            || (order.Status is ScreeningOrderStatus.Completed
                or ScreeningOrderStatus.Canceled
                or ScreeningOrderStatus.Failed
                && !reusePaidFailure))
        {
            order = ScreeningOrder.Create(
                app.ApplicationId,
                request.RequestingUserId,
                app.TenantUserId,
                app.ListingId,
                screeningProvider.ProviderKey);
            dbContext.ScreeningOrders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var fee = await platformSettings
            .GetLongAsync(PlatformSettingKeys.ScreeningHostFeeCents, screeningOptions.Value.HostFeeCents, cancellationToken)
            .ConfigureAwait(false);
        if (fee <= 0)
        {
            order.MarkPaid();
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            var placedFree = await mediator
                .Send(new PlaceProviderScreeningOrderCommand(order.Id), CancellationToken.None)
                .ConfigureAwait(false);
            if (placedFree.IsFailure)
            {
                return Result<ScreeningCheckoutDto>.Failure(placedFree.Error);
            }

            return Result<ScreeningCheckoutDto>.Success(
                new ScreeningCheckoutDto(string.Empty, string.Empty, "succeeded", 0, Currency, order.Id));
        }

        using var stripeCts = new CancellationTokenSource(TimeSpan.FromSeconds(55));

        if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
        {
            var existing = await stripeService
                .GetPaymentIntentAsync(order.StripePaymentIntentId, stripeCts.Token)
                .ConfigureAwait(false);

            if (existing.Status is "succeeded")
            {
                order.MarkPaid();
                await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                var placedPaid = await mediator
                    .Send(new PlaceProviderScreeningOrderCommand(order.Id), CancellationToken.None)
                    .ConfigureAwait(false);
                if (placedPaid.IsFailure)
                {
                    return Result<ScreeningCheckoutDto>.Failure(placedPaid.Error);
                }

                return Result<ScreeningCheckoutDto>.Success(
                    new ScreeningCheckoutDto(
                        string.Empty, existing.PaymentIntentId, existing.Status,
                        existing.Amount, existing.Currency, order.Id));
            }

            if (PayableStatuses.Contains(existing.Status))
            {
                return Result<ScreeningCheckoutDto>.Success(
                    new ScreeningCheckoutDto(
                        existing.ClientSecret, existing.PaymentIntentId, existing.Status,
                        existing.Amount, existing.Currency, order.Id));
            }
        }

        var metadata = new Dictionary<string, string>
        {
            [ScreeningPaymentMetadata.PurposeKey] = ScreeningPaymentMetadata.PurposeValue,
            [ScreeningPaymentMetadata.OrderIdKey] = order.Id.ToString(),
            [ScreeningPaymentMetadata.ApplicationIdKey] = app.ApplicationId.ToString(),
            [ScreeningPaymentMetadata.HostUserIdKey] = request.RequestingUserId.ToString()
        };

        var pi = await stripeService.CreatePlatformPaymentIntentAsync(
            fee,
            Currency,
            metadata,
            idempotencyKey: $"screening-{order.Id}",
            stripeCts.Token).ConfigureAwait(false);

        order.RecordPaymentIntent(pi.PaymentIntentId);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return Result<ScreeningCheckoutDto>.Success(
            new ScreeningCheckoutDto(
                pi.ClientSecret, pi.PaymentIntentId, pi.Status, pi.Amount, pi.Currency, order.Id));
    }
}
