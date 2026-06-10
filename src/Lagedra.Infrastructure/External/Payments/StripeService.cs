using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Lagedra.Infrastructure.External.Payments;

public sealed partial class StripeService(
    IOptions<StripeSettings> settings,
    ILogger<StripeService> logger)
    : IStripeService
{
    private readonly StripeSettings _settings = settings.Value;

    private RequestOptions NewRequestOptions(string? idempotencyKey = null) =>
        new() { ApiKey = _settings.SecretKey, IdempotencyKey = idempotencyKey };

    public async Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var service = new CustomerService();
        var existing = await service.ListAsync(new CustomerListOptions { Email = email, Limit = 1 }, opts, ct).ConfigureAwait(false);

        if (existing.Data.Count > 0)
        {
            return existing.Data[0].Id;
        }

        var created = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
        }, opts, ct).ConfigureAwait(false);

        LogCustomerCreated(logger, userId, created.Id);
        return created.Id;
    }

    public async Task<StripeSubscriptionResult> CreateSubscriptionAsync(string customerId, string priceId, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = ["latest_invoice.payment_intent"]
        }, opts, ct).ConfigureAwait(false);

        var clientSecret = subscription.LatestInvoice?.PaymentIntent?.ClientSecret ?? string.Empty;
        LogSubscriptionCreated(logger, customerId, subscription.Id);
        return new StripeSubscriptionResult(subscription.Id, clientSecret, subscription.CurrentPeriodEnd);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var service = new SubscriptionService();
        await service.CancelAsync(subscriptionId, null, opts, ct).ConfigureAwait(false);
        LogSubscriptionCancelled(logger, subscriptionId);
    }

    public async Task<StripeInvoiceResult> CreateProratedInvoiceAsync(string subscriptionId, string priceId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(subscriptionId, null, opts, ct).ConfigureAwait(false);

        var itemService = new SubscriptionItemService();
        var items = await itemService.ListAsync(new SubscriptionItemListOptions { Subscription = subscriptionId }, opts, ct).ConfigureAwait(false);

        await subService.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
        {
            Items = items.Data.Select(i => new SubscriptionItemOptions
            {
                Id = i.Id,
                Price = priceId
            }).ToList(),
            ProrationBehavior = "create_prorations"
        }, opts, ct).ConfigureAwait(false);

        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = sub.CustomerId,
            Subscription = subscriptionId
        }, opts, ct).ConfigureAwait(false);

        return new StripeInvoiceResult(invoice.Id, invoice.AmountDue, invoice.Currency);
    }

    public Task<Event> VerifyWebhookAsync(string payload, string signature)
    {
        var webhookEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
        LogWebhookReceived(logger, webhookEvent.Type, webhookEvent.Id);
        return Task.FromResult(webhookEvent);
    }

    public async Task<StripeConnectedAccountResult> CreateConnectedAccountAsync(Guid hostUserId, string email, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var accountService = new AccountService();

        var account = await accountService.CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Email = email,
            Metadata = new Dictionary<string, string> { ["hostUserId"] = hostUserId.ToString() },
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            }
        }, opts, ct).ConfigureAwait(false);

        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = account.Id,
            RefreshUrl = _settings.ConnectRefreshUrl.ToString(),
            ReturnUrl = _settings.ConnectReturnUrl.ToString(),
            Type = "account_onboarding"
        }, opts, ct).ConfigureAwait(false);

        LogConnectedAccountCreated(logger, hostUserId, account.Id);
        return new StripeConnectedAccountResult(account.Id, new Uri(link.Url));
    }

    public async Task<Uri> CreateAccountOnboardingLinkAsync(string accountId, Uri? returnUrl = null, Uri? refreshUrl = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var linkService = new AccountLinkService();

        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            RefreshUrl = (refreshUrl ?? _settings.ConnectRefreshUrl).ToString(),
            ReturnUrl = (returnUrl ?? _settings.ConnectReturnUrl).ToString(),
            Type = "account_onboarding"
        }, opts, ct).ConfigureAwait(false);

        return new Uri(link.Url);
    }

    public async Task<StripeAccountStatusResult> GetAccountStatusAsync(string accountId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var accountService = new AccountService();

        var account = await accountService.GetAsync(accountId, null, opts, ct).ConfigureAwait(false);

        return new StripeAccountStatusResult(
            account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted);
    }

    public async Task<StripePaymentIntentResult> CreateDestinationPaymentIntentAsync(
        long amountCents, string currency, string destinationAccountId,
        long applicationFeeCents, Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            ApplicationFeeAmount = applicationFeeCents,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId
            },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = metadata ?? []
        };

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogPaymentIntentCreated(logger, pi.Id, amountCents, destinationAccountId);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripePaymentIntentResult> CreatePlatformPaymentIntentAsync(
        long amountCents, string currency,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = metadata ?? []
        };

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogPaymentIntentCreated(logger, pi.Id, amountCents, "platform");
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var service = new PaymentIntentService();

        var pi = await service.GetAsync(paymentIntentId, null, opts, ct).ConfigureAwait(false);

        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripeRefundResult> RefundPaymentIntentAsync(string paymentIntentId, long? amountCents = null, string? idempotencyKey = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        var opts = NewRequestOptions(idempotencyKey);
        var service = new RefundService();

        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId
        };

        if (amountCents.HasValue)
        {
            options.Amount = amountCents.Value;
        }

        var refund = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogRefundCreated(logger, refund.Id, paymentIntentId, refund.Amount);
        return new StripeRefundResult(refund.Id, refund.Amount, refund.Status);
    }

    public async Task<bool> CheckConnectivityAsync(CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var service = new BalanceService();
        var balance = await service.GetAsync(opts, ct).ConfigureAwait(false);
        return balance is not null;
    }

    public async Task<StripeSetupIntentResult> CreateSetupIntentAsync(
        string customerId,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var opts = NewRequestOptions(idempotencyKey);
        var service = new SetupIntentService();

        var options = new SetupIntentCreateOptions
        {
            Customer = customerId,
            Usage = "off_session",
            AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = metadata ?? [],
        };

        var setupIntent = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogSetupIntentCreated(logger, setupIntent.Id, customerId);
        return new StripeSetupIntentResult(setupIntent.Id, setupIntent.ClientSecret, setupIntent.Status);
    }

    public async Task<StripePaymentIntentResult> ChargeOffSessionPlatformAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodId);

        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        // Off-session charge: confirm immediately, do not show 3DS UI. If the
        // card requires authentication Stripe returns a "requires_action"
        // PaymentIntent and the caller can fall back to surfacing the
        // checkout flow for that one-off case.
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Customer = customerId,
            PaymentMethod = paymentMethodId,
            Confirm = true,
            OffSession = true,
            Metadata = metadata ?? [],
        };

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogOffSessionChargeCreated(logger, pi.Id, amountCents, customerId);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripePaymentIntentResult> ChargeOffSessionDestinationAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        string destinationAccountId,
        long applicationFeeCents,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationAccountId);

        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Customer = customerId,
            PaymentMethod = paymentMethodId,
            ApplicationFeeAmount = applicationFeeCents,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId,
            },
            Confirm = true,
            OffSession = true,
            Metadata = metadata ?? [],
        };

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogOffSessionChargeCreated(logger, pi.Id, amountCents, customerId);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe customer created for user {UserId}: {CustomerId}")]
    private static partial void LogCustomerCreated(ILogger logger, Guid userId, string customerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription created for customer {CustomerId}: {SubscriptionId}")]
    private static partial void LogSubscriptionCreated(ILogger logger, string customerId, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription cancelled: {SubscriptionId}")]
    private static partial void LogSubscriptionCancelled(ILogger logger, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe webhook received: {EventType} ({EventId})")]
    private static partial void LogWebhookReceived(ILogger logger, string eventType, string eventId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe connected account created for host {HostUserId}: {AccountId}")]
    private static partial void LogConnectedAccountCreated(ILogger logger, Guid hostUserId, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe PaymentIntent created: {PaymentIntentId}, amount {AmountCents}, destination {DestinationAccountId}")]
    private static partial void LogPaymentIntentCreated(ILogger logger, string paymentIntentId, long amountCents, string destinationAccountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe refund created: {RefundId} for PI {PaymentIntentId}, amount {AmountCents}")]
    private static partial void LogRefundCreated(ILogger logger, string refundId, string paymentIntentId, long amountCents);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe SetupIntent created: {SetupIntentId} for customer {CustomerId}")]
    private static partial void LogSetupIntentCreated(ILogger logger, string setupIntentId, string customerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe off-session charge created: {PaymentIntentId} amount {AmountCents} customer {CustomerId}")]
    private static partial void LogOffSessionChargeCreated(ILogger logger, string paymentIntentId, long amountCents, string customerId);
}
