using Lagedra.Infrastructure.External.Payments;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnDealActivatedCreateHostSubscriptionHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IUserEmailResolver emailResolver,
    IPlatformSettingsService settings,
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
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "StripePlatformFeePriceId not configured — skipping subscription for deal {DealId}")]
    private static partial void LogMissingPriceId(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not resolve email for host {HostUserId} — skipping subscription")]
    private static partial void LogMissingHostEmail(ILogger logger, Guid hostUserId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created host platform fee subscription for deal {DealId}: {SubscriptionId}")]
    private static partial void LogSubscriptionCreated(ILogger logger, Guid dealId, string subscriptionId);
}
