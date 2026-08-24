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

    // Restricts the PaymentElement to the configured allow-list. When a list is
    // set we pass explicit payment_method_types (a hard whitelist) and must NOT
    // also enable automatic_payment_methods — Stripe rejects setting both. An
    // empty list means "let Stripe decide" (automatic methods, dashboard/geo),
    // which is what surfaced wrong-region rails (Pix, Kakao Pay, Naver Pay).
    private void ApplyPaymentMethodSelection(PaymentIntentCreateOptions options)
    {
        if (_settings.PaymentMethodTypes is { Count: > 0 } allowed)
        {
            options.PaymentMethodTypes = [.. allowed];
        }
        else
        {
            options.AutomaticPaymentMethods =
                new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true };
        }
    }

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

    public async Task<string> EnsureCustomerAsync(
        Guid ownerId,
        string email,
        string? existingCustomerId,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(existingCustomerId))
        {
            try
            {
                var opts = NewRequestOptions();
                var service = new CustomerService();
                await service.GetAsync(existingCustomerId, null, opts, ct).ConfigureAwait(false);
                return existingCustomerId;
            }
            catch (StripeException ex) when (IsMissingResource(ex))
            {
                LogStaleCustomerIgnored(logger, existingCustomerId, ownerId);
            }
        }

        return await GetOrCreateCustomerAsync(ownerId, email, ct).ConfigureAwait(false);
    }

    private static bool IsMissingResource(StripeException ex) =>
        string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.Ordinal)
        || (ex.Message?.Contains("No such customer", StringComparison.OrdinalIgnoreCase) ?? false);

    public async Task<StripeSubscriptionResult> CreateSubscriptionAsync(string customerId, string priceId, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new SubscriptionService();
        // Invoice-based collection: hosts have no saved card at activation, so
        // an auto-collected subscription (the previous default_incomplete
        // setup) sat in `incomplete` with a payment link nobody surfaced and
        // silently voided after ~23 hours — the protocol fee never billed.
        // With send_invoice Stripe opens an invoice due in 7 days that the
        // host pays on Stripe's hosted page; the card used there is saved as
        // the subscription default for later cycles, and Stripe's own
        // reminder/dunning emails apply when enabled in the dashboard.
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
            CollectionMethod = "send_invoice",
            DaysUntilDue = 7,
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = ["latest_invoice"]
        }, opts, ct).ConfigureAwait(false);

        // send_invoice creates the first invoice as a draft (Stripe would
        // auto-finalize it about an hour later). Finalize it now so the
        // hosted payment URL exists immediately and the activation email can
        // include it. Fresh request options: reusing the subscription's
        // idempotency key here would collide with the create call above.
        var invoice = subscription.LatestInvoice;
        if (invoice is not null && invoice.Status == "draft")
        {
            var invoiceService = new InvoiceService();
            invoice = await invoiceService
                .FinalizeInvoiceAsync(invoice.Id, options: null, NewRequestOptions(), ct)
                .ConfigureAwait(false);
        }

        LogSubscriptionCreated(logger, customerId, subscription.Id);

        Uri? hostedInvoiceUrl = null;
        if (!string.IsNullOrEmpty(invoice?.HostedInvoiceUrl))
        {
            Uri.TryCreate(invoice.HostedInvoiceUrl, UriKind.Absolute, out hostedInvoiceUrl);
        }

        return new StripeSubscriptionResult(
            subscription.Id,
            invoice?.PaymentIntent?.ClientSecret ?? string.Empty,
            subscription.CurrentPeriodEnd,
            hostedInvoiceUrl);
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

    public async Task<StripeConnectedAccountResult> CreateConnectedAccountAsync(
        Guid hostUserId,
        string email,
        Uri? returnUrl = null,
        Uri? refreshUrl = null,
        CancellationToken ct = default)
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
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                // Required for destination charges with on_behalf_of: the connected
                // account is the settlement merchant of record, so it must be able to
                // accept card payments (non-custodial model — Option A).
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true }
            }
        }, opts, ct).ConfigureAwait(false);

        var resolvedReturn = returnUrl ?? _settings.ConnectReturnUrl;
        var resolvedRefresh = refreshUrl ?? _settings.ConnectRefreshUrl;

        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = account.Id,
            RefreshUrl = resolvedRefresh.ToString(),
            ReturnUrl = resolvedReturn.ToString(),
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

    public async Task<Uri> CreateAccountUpdateLinkAsync(string accountId, Uri? returnUrl = null, Uri? refreshUrl = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var opts = NewRequestOptions();
        var linkService = new AccountLinkService();

        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            RefreshUrl = (refreshUrl ?? _settings.ConnectRefreshUrl).ToString(),
            ReturnUrl = (returnUrl ?? _settings.ConnectReturnUrl).ToString(),
            Type = "account_update"
        }, opts, ct).ConfigureAwait(false);

        LogAccountUpdateLinkCreated(logger, accountId);
        return new Uri(link.Url);
    }

    public async Task<Uri> CreateConnectActionLinkAsync(
        string accountId,
        Uri? returnUrl = null,
        Uri? refreshUrl = null,
        CancellationToken ct = default)
    {
        var status = await GetAccountStatusAsync(accountId, ct).ConfigureAwait(false);
        if (status.DetailsSubmitted)
        {
            return await CreateAccountUpdateLinkAsync(accountId, returnUrl, refreshUrl, ct)
                .ConfigureAwait(false);
        }

        return await CreateAccountOnboardingLinkAsync(accountId, returnUrl, refreshUrl, ct)
            .ConfigureAwait(false);
    }

    public async Task<Uri> CreateExpressLoginLinkAsync(string accountId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var opts = NewRequestOptions();
        var loginLinkService = new AccountLoginLinkService();

        var loginLink = await loginLinkService
            .CreateAsync(accountId, null, opts, ct)
            .ConfigureAwait(false);

        LogExpressLoginLinkCreated(logger, accountId);
        return new Uri(loginLink.Url);
    }

    public async Task<StripeAccountStatusResult> GetAccountStatusAsync(string accountId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var accountService = new AccountService();

        var account = await accountService.GetAsync(
            accountId,
            new AccountGetOptions { Expand = ["external_accounts"] },
            opts,
            ct).ConfigureAwait(false);

        var requirements = account.Requirements;
        var currentlyDue = requirements?.CurrentlyDue ?? [];
        var pastDue = requirements?.PastDue ?? [];
        var pendingVerification = requirements?.PendingVerification ?? [];

        var hasOutstandingTaxRequirement =
            currentlyDue.Any(IsTaxRequirement) || pastDue.Any(IsTaxRequirement);
        var taxRequirementPastDue = pastDue.Any(IsTaxRequirement);
        var taxRequirementPendingVerification = pendingVerification.Any(IsTaxRequirement);
        var hasOutstandingBankRequirement =
            currentlyDue.Any(IsBankRequirement) || pastDue.Any(IsBankRequirement);
        var bankRequirementPastDue = pastDue.Any(IsBankRequirement);
        var isRestricted = !string.IsNullOrEmpty(requirements?.DisabledReason);
        var hasExternalAccount = account.ExternalAccounts?.Data?.Count > 0;

        return new StripeAccountStatusResult(
            account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted,
            hasExternalAccount,
            hasOutstandingTaxRequirement,
            taxRequirementPastDue,
            taxRequirementPendingVerification,
            isRestricted,
            hasOutstandingBankRequirement,
            bankRequirementPastDue,
            currentlyDue.ToList(),
            pastDue.ToList(),
            pendingVerification.ToList(),
            requirements?.DisabledReason);
    }

    // Stripe expresses tax-form needs (W-9/W-8, EIN/SSN) as requirement keys such
    // as "company.tax_id", "individual.id_number", "individual.ssn_last_4". Match
    // on the stable token fragments so new variants are still classified as tax.
    private static bool IsTaxRequirement(string requirement) =>
        requirement.Contains("tax_id", StringComparison.OrdinalIgnoreCase)
        || requirement.Contains("id_number", StringComparison.OrdinalIgnoreCase)
        || requirement.Contains("ssn", StringComparison.OrdinalIgnoreCase);

    private static bool IsBankRequirement(string requirement) =>
        requirement.Contains("external_account", StringComparison.OrdinalIgnoreCase);

    public async Task<StripePaymentIntentResult> CreateDestinationPaymentIntentAsync(
        long amountCents, string currency, string destinationAccountId,
        string onBehalfOf, long applicationFeeCents, Dictionary<string, string>? metadata = null,
        string? statementDescriptorSuffix = null, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            ApplicationFeeAmount = applicationFeeCents,
            // on_behalf_of makes the connected account the settlement merchant of
            // record, so the host's rent/deposit never settles into the platform
            // balance (non-custodial — Option A).
            OnBehalfOf = onBehalfOf,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId
            },
            Metadata = metadata ?? []
        };

        ApplyPaymentMethodSelection(options);

        // Card payments with on_behalf_of can only set a dynamic suffix; the host's
        // descriptor is the static component shown to the customer.
        var suffix = statementDescriptorSuffix ?? _settings.StatementDescriptorSuffix;
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            options.StatementDescriptorSuffix = suffix;
        }

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogPaymentIntentCreated(logger, pi.Id, amountCents, destinationAccountId);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripePaymentIntentResult> CreatePlatformPaymentIntentAsync(
        long amountCents, string currency, Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default)
    {
        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        // No TransferData / OnBehalfOf / ApplicationFee: the funds settle into the
        // platform balance because this is the platform's own service fee.
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Metadata = metadata ?? []
        };

        ApplyPaymentMethodSelection(options);

        var suffix = _settings.StatementDescriptorSuffix;
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            options.StatementDescriptorSuffix = suffix;
        }

        var pi = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogPlatformPaymentIntentCreated(logger, pi.Id, amountCents);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var opts = NewRequestOptions();
        var service = new PaymentIntentService();

        var pi = await service.GetAsync(paymentIntentId, null, opts, ct).ConfigureAwait(false);

        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status, pi.Amount, pi.Currency);
    }

    public async Task<StripeRefundResult> RefundPaymentIntentAsync(
        string paymentIntentId,
        long? amountCents = null,
        bool reverseTransfer = false,
        bool refundApplicationFee = false,
        string? idempotencyKey = null,
        CancellationToken ct = default)
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

        // For destination charges the money is in the host's connected account, so
        // pull it back on refund. RefundApplicationFee also returns the platform's cut.
        if (reverseTransfer)
        {
            options.ReverseTransfer = true;
        }

        if (refundApplicationFee)
        {
            options.RefundApplicationFee = true;
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

    public async Task<long?> GetPriceAmountCentsAsync(string priceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(priceId);

        var opts = NewRequestOptions();
        var service = new PriceService();
        var price = await service.GetAsync(priceId, null, opts, ct).ConfigureAwait(false);
        return price.UnitAmount;
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
            // Card-on-file is charged off-session the moment the host approves,
            // which is a card flow — so save cards only. This also keeps the
            // wrong-region rails (Pix/Kakao Pay/Naver Pay) out of the apply-time
            // PaymentElement, matching the checkout allow-list.
            PaymentMethodTypes = ["card"],
            Metadata = metadata ?? [],
        };

        var setupIntent = await service.CreateAsync(options, opts, ct).ConfigureAwait(false);
        LogSetupIntentCreated(logger, setupIntent.Id, customerId);
        return new StripeSetupIntentResult(setupIntent.Id, setupIntent.ClientSecret, setupIntent.Status);
    }

    public async Task<StripePaymentIntentResult> ChargeOffSessionDestinationAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        string destinationAccountId,
        string onBehalfOf,
        long applicationFeeCents,
        Dictionary<string, string>? metadata = null,
        string? statementDescriptorSuffix = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(onBehalfOf);

        var opts = NewRequestOptions(idempotencyKey);
        var service = new PaymentIntentService();

        // Off-session charge: confirm immediately, do not show 3DS UI. If the
        // card requires authentication Stripe returns a "requires_action"
        // PaymentIntent and the caller falls back to surfacing the checkout flow.
        // on_behalf_of makes the host the settlement merchant of record so the
        // platform never holds the rent/deposit (non-custodial — Option A).
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Customer = customerId,
            PaymentMethod = paymentMethodId,
            ApplicationFeeAmount = applicationFeeCents,
            OnBehalfOf = onBehalfOf,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId,
            },
            Confirm = true,
            OffSession = true,
            Metadata = metadata ?? [],
        };

        // Card payments with on_behalf_of can only set a dynamic suffix; the host's
        // descriptor is the static component shown to the customer.
        var suffix = statementDescriptorSuffix ?? _settings.StatementDescriptorSuffix;
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            options.StatementDescriptorSuffix = suffix;
        }

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe Express login link created for account {AccountId}")]
    private static partial void LogExpressLoginLinkCreated(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe account_update link created for account {AccountId}")]
    private static partial void LogAccountUpdateLinkCreated(ILogger logger, string accountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe PaymentIntent created: {PaymentIntentId}, amount {AmountCents}, destination {DestinationAccountId}")]
    private static partial void LogPaymentIntentCreated(ILogger logger, string paymentIntentId, long amountCents, string destinationAccountId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe platform PaymentIntent created: {PaymentIntentId}, amount {AmountCents}")]
    private static partial void LogPlatformPaymentIntentCreated(ILogger logger, string paymentIntentId, long amountCents);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe refund created: {RefundId} for PI {PaymentIntentId}, amount {AmountCents}")]
    private static partial void LogRefundCreated(ILogger logger, string refundId, string paymentIntentId, long amountCents);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe SetupIntent created: {SetupIntentId} for customer {CustomerId}")]
    private static partial void LogSetupIntentCreated(ILogger logger, string setupIntentId, string customerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cached Stripe customer {CustomerId} for owner {OwnerId} is missing on this Stripe account — recreating")]
    private static partial void LogStaleCustomerIgnored(ILogger logger, string customerId, Guid ownerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe off-session charge created: {PaymentIntentId} amount {AmountCents} customer {CustomerId}")]
    private static partial void LogOffSessionChargeCreated(ILogger logger, string paymentIntentId, long amountCents, string customerId);
}
