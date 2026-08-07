using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Creates a Stripe SetupIntent against the partner organization's Stripe Customer
/// so the company can pay on a member's behalf.
/// </summary>
public sealed record CreatePartnerBookingSetupIntentCommand(
    Guid OrganizationId,
    Guid ListingId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<PartnerBookingSetupIntentResult>>;

public sealed record PartnerBookingSetupIntentResult(
    string SetupIntentId,
    string ClientSecret,
    string CustomerId);

public sealed partial class CreatePartnerBookingSetupIntentCommandHandler(
    IPartnerAccessService accessService,
    IPartnerOrganizationBillingProfile orgBilling,
    IStripeService stripeService,
    ILogger<CreatePartnerBookingSetupIntentCommandHandler> logger)
    : IRequestHandler<CreatePartnerBookingSetupIntentCommand, Result<PartnerBookingSetupIntentResult>>
{
    public async Task<Result<PartnerBookingSetupIntentResult>> Handle(
        CreatePartnerBookingSetupIntentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authz = await accessService.RequireVerifiedOrgAdminAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authz.IsFailure)
        {
            return Result<PartnerBookingSetupIntentResult>.Failure(authz.Error);
        }

        var orgName = await orgBilling
            .GetNameAsync(request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(orgName))
        {
            return Result<PartnerBookingSetupIntentResult>.Failure(
                new Error("Partner.NotFound", "Partner organization not found."));
        }

        var existingCustomerId = await orgBilling
            .GetStripeCustomerIdAsync(request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        var customerId = await stripeService
            .EnsureCustomerAsync(
                request.OrganizationId,
                $"partner-{request.OrganizationId:N}@lagedra.partners",
                existingCustomerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.Equals(existingCustomerId, customerId, StringComparison.Ordinal))
        {
            await orgBilling
                .SetStripeCustomerIdAsync(request.OrganizationId, customerId, cancellationToken)
                .ConfigureAwait(false);

            LogCustomerCached(logger, request.OrganizationId, customerId);
        }

        var idempotencyKey =
            $"si-partner-{request.OrganizationId:N}-{request.ListingId:N}-{request.CallerUserId:N}-{customerId}";

        var setupIntent = await stripeService
            .CreateSetupIntentAsync(
                customerId,
                new Dictionary<string, string>
                {
                    ["partnerOrganizationId"] = request.OrganizationId.ToString(),
                    ["listingId"] = request.ListingId.ToString(),
                    ["payerUserId"] = request.CallerUserId.ToString(),
                    ["purpose"] = "partner-booking-card-on-file",
                },
                idempotencyKey,
                cancellationToken)
            .ConfigureAwait(false);

        return Result<PartnerBookingSetupIntentResult>.Success(
            new PartnerBookingSetupIntentResult(
                setupIntent.SetupIntentId,
                setupIntent.ClientSecret,
                customerId));
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cached Stripe customer {CustomerId} on partner org {OrganizationId}")]
    private static partial void LogCustomerCached(
        ILogger logger, Guid organizationId, string customerId);
}
