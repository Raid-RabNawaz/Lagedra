using Lagedra.Infrastructure.External.Payments;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Phase 16.9 — pre-flight command issued by the Apply dialog so it can
/// mount Stripe Elements in "save card" mode. Returns the SetupIntent
/// client_secret plus the cached Stripe customer id (lazily created
/// the first time a tenant books). The PaymentMethod id is captured by
/// the frontend after <c>stripe.confirmSetup</c> resolves and is then
/// passed back through <see cref="SubmitApplicationCommand"/>.
/// </summary>
public sealed record CreateBookingSetupIntentCommand(
    Guid TenantUserId,
    Guid ListingId) : IRequest<Result<BookingSetupIntentResult>>;

public sealed record BookingSetupIntentResult(
    string SetupIntentId,
    string ClientSecret,
    string CustomerId);

public sealed partial class CreateBookingSetupIntentCommandHandler(
    IStripeService stripeService,
    IUserStripeProfileService userStripeProfile,
    ILogger<CreateBookingSetupIntentCommandHandler> logger)
    : IRequestHandler<CreateBookingSetupIntentCommand, Result<BookingSetupIntentResult>>
{
    public async Task<Result<BookingSetupIntentResult>> Handle(
        CreateBookingSetupIntentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await userStripeProfile
            .GetAsync(request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<BookingSetupIntentResult>.Failure(
                new Error("BookingSetup.UserNotFound",
                    "Could not resolve the tenant user for SetupIntent creation."));
        }

        var customerId = profile.StripeCustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            customerId = await stripeService
                .GetOrCreateCustomerAsync(profile.UserId, profile.Email, cancellationToken)
                .ConfigureAwait(false);

            await userStripeProfile
                .SetStripeCustomerIdAsync(profile.UserId, customerId, cancellationToken)
                .ConfigureAwait(false);

            LogCustomerCachedOnUser(logger, profile.UserId, customerId);
        }

        // Idempotency keyed on the (tenant, listing) tuple keeps the
        // SetupIntent stable if the dialog is opened twice in quick
        // succession (e.g. modal re-mounts on hot reload).
        var idempotencyKey = $"si-apply-{request.TenantUserId:N}-{request.ListingId:N}";
        var setupIntent = await stripeService
            .CreateSetupIntentAsync(
                customerId,
                metadata: new Dictionary<string, string>
                {
                    ["tenantUserId"] = request.TenantUserId.ToString(),
                    ["listingId"] = request.ListingId.ToString(),
                    ["purpose"] = "booking-card-on-file",
                },
                idempotencyKey: idempotencyKey,
                ct: cancellationToken)
            .ConfigureAwait(false);

        return Result<BookingSetupIntentResult>.Success(
            new BookingSetupIntentResult(
                setupIntent.SetupIntentId,
                setupIntent.ClientSecret,
                customerId));
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cached Stripe customer {CustomerId} on user {UserId} during booking pre-flight")]
    private static partial void LogCustomerCachedOnUser(
        ILogger logger, Guid userId, string customerId);
}
