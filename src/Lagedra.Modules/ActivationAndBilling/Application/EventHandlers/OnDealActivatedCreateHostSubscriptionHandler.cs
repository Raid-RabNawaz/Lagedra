using Lagedra.Infrastructure.External.Payments;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnDealActivatedCreateHostSubscriptionHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IUserEmailResolver emailResolver,
    IPlatformSettingsService settings,
    IMediator mediator,
    ILogger<OnDealActivatedCreateHostSubscriptionHandler> logger)
    : IDomainEventHandler<DealActivatedEvent>
{
    public async Task Handle(DealActivatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.Id == domainEvent.BillingAccountId, ct)
            .ConfigureAwait(false);

        if (account is null || !string.IsNullOrEmpty(account.StripeSubscriptionId))
        {
            return;
        }

        var priceId = await settings
            .GetStringAsync(PlatformSettingKeys.StripePlatformFeePriceId, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(priceId))
        {
            LogMissingPriceId(logger, domainEvent.DealId);
            return;
        }

        var hostEmail = await emailResolver
            .GetEmailAsync(domainEvent.LandlordUserId, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(hostEmail))
        {
            LogMissingHostEmail(logger, domainEvent.LandlordUserId);
            return;
        }

        var customerId = await stripeService
            .GetOrCreateCustomerAsync(domainEvent.LandlordUserId, hostEmail, ct)
            .ConfigureAwait(false);

        account.SetStripeCustomerId(customerId);

        var idempotencyKey = $"sub-deal-{domainEvent.DealId}";
        var subscription = await stripeService
            .CreateSubscriptionAsync(customerId, priceId, idempotencyKey, ct)
            .ConfigureAwait(false);

        account.SetStripeSubscriptionId(subscription.SubscriptionId);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogSubscriptionCreated(logger, domainEvent.DealId, subscription.SubscriptionId);

        await NotifyHostOfFirstInvoiceAsync(domainEvent, subscription, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Emails the host the Stripe hosted payment link for the first protocol
    /// fee invoice. Without this nothing tells the host to pay: the
    /// subscription is invoice-collected because no card is on file yet.
    /// Best-effort — the subscription exists and Stripe's own reminders and
    /// the enforcement job chase unpaid invoices, so a notification failure
    /// must not fail (and re-run) the activation handler.
    /// </summary>
    private async Task NotifyHostOfFirstInvoiceAsync(
        DealActivatedEvent domainEvent,
        StripeSubscriptionResult subscription,
        CancellationToken ct)
    {
        if (subscription.HostedInvoiceUrl is null)
        {
            LogMissingInvoiceUrl(logger, domainEvent.DealId);
            return;
        }

        try
        {
            await mediator.Send(new NotifyUserCommand(
                domainEvent.LandlordUserId,
                "protocol_fee_invoice",
                "Protocol Fee Invoice Ready",
                "Your booking is active. Pay your first monthly protocol fee invoice to keep "
                + "your account in good standing — the payment link is in your email.",
                new()
                {
                    ["dealId"] = domainEvent.DealId.ToString(),
                    ["invoiceUrl"] = subscription.HostedInvoiceUrl.ToString(),
                },
                [NotificationChannel.Email, NotificationChannel.InApp],
                domainEvent.DealId,
                "Deal"), ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // best-effort: see remarks above
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogInvoiceNotificationFailed(logger, domainEvent.DealId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "StripePlatformFeePriceId not configured — skipping subscription for deal {DealId}")]
    private static partial void LogMissingPriceId(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not resolve email for host {HostUserId} — skipping subscription")]
    private static partial void LogMissingHostEmail(ILogger logger, Guid hostUserId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created host platform fee subscription for deal {DealId}: {SubscriptionId}")]
    private static partial void LogSubscriptionCreated(ILogger logger, Guid dealId, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No hosted invoice URL for deal {DealId}'s protocol fee subscription — host was not emailed a payment link")]
    private static partial void LogMissingInvoiceUrl(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to notify host about the first protocol fee invoice for deal {DealId}")]
    private static partial void LogInvoiceNotificationFailed(ILogger logger, Guid dealId, Exception ex);
}
