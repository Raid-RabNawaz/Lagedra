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

        // After a Stripe account migration, cached cus_ ids from the previous
        // platform are invalid. EnsureCustomer recreates when resource_missing.
        var customerId = await stripeService
            .EnsureCustomerAsync(
                profile.UserId,
                profile.Email,
                profile.StripeCustomerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.Equals(profile.StripeCustomerId, customerId, StringComparison.Ordinal))
        {
            await userStripeProfile
                .SetStripeCustomerIdAsync(profile.UserId, customerId, cancellationToken)
                .ConfigureAwait(false);

            LogCustomerCachedOnUser(logger, profile.UserId, customerId);
        }

        // Include customer id so a stale idempotency key from a failed attempt
        // against a missing customer does not keep returning the cached error.
        var idempotencyKey =
            $"si-apply-{request.TenantUserId:N}-{request.ListingId:N}-{customerId}";
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
