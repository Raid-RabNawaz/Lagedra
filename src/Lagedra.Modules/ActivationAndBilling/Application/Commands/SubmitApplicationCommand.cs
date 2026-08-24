using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record SubmitApplicationCommand(
    Guid ListingId,
    Guid TenantUserId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int GuestCount = 1,
    string? Message = null,
    string? StripePaymentMethodId = null,
    bool TruthSurfaceConsentGiven = false,
    string? ConsentVersion = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<SubmitApplicationResult>>;

/// <summary>
/// Result of a reservation request. Under the predetermined-deposit flow the
/// request always lands as <c>Pending</c> for the host to accept, except when
/// the listing has instant booking enabled — then the command auto-approves
/// (which seals the Truth Surface and charges off-session) in the same call.
/// <see cref="NextPath"/> tells the UI where to send the tenant next.
/// </summary>
public sealed record SubmitApplicationResult(
    DealApplicationDto Application,
    string NextPath);

public sealed partial class SubmitApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IReservationPricingService reservationPricingService,
    IHostStripeAccountProvider hostStripeAccountProvider,
    IFeatureFlags featureFlags,
    IMediator mediator,
    IAuditTrailWriter auditTrail,
    ILogger<SubmitApplicationCommandHandler> logger)
    : IRequestHandler<SubmitApplicationCommand, Result<SubmitApplicationResult>>
{
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error DatesOutOfRange = new("Dates.OutOfStayRange", "Requested dates fall outside the listing's allowed stay range.");
    private static readonly Error DatesUnavailable = new("Dates.Unavailable", "The requested dates are not available.");
    private static readonly Error OwnListing = new("Application.OwnListing", "You cannot apply to your own listing.");
    private static readonly Error DuplicateActiveRequest = new(
        "Application.DuplicateActiveRequest",
        "You already have a booking request or active booking on this listing for overlapping dates. " +
        "Cancel or wait on that one before requesting these dates again.");
    private static readonly Error GuestCountInvalid = new(
        "Application.GuestCountInvalid", "Guest count must be at least 1.");
    private static readonly Error GuestCountExceedsMax = new(
        "Application.GuestCountExceedsMax",
        "Requested guest count exceeds the listing's maximum guests.");
    private static readonly Error MessageTooLong = new(
        "Application.MessageTooLong",
        $"Message must be {DealApplication.MessageMaxLength} characters or fewer.");
    private static readonly Error ConsentRequired = new(
        "Application.ConsentRequired",
        "You must agree to the Truth Surface terms to submit a reservation request.");
    private static readonly Error PaymentMethodRequired = new(
        "Application.PaymentMethodRequired",
        "A payment method is required to submit a reservation request.");

    public async Task<Result<SubmitApplicationResult>> Handle(
        SubmitApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<SubmitApplicationResult>.Failure(ListingNotFound);
        }

        if (listing.LandlordUserId == request.TenantUserId)
        {
            return Result<SubmitApplicationResult>.Failure(OwnListing);
        }

        if (request.GuestCount < 1)
        {
            return Result<SubmitApplicationResult>.Failure(GuestCountInvalid);
        }
        if (listing.HouseRules is { MaxGuests: > 0 } && request.GuestCount > listing.HouseRules.MaxGuests)
        {
            return Result<SubmitApplicationResult>.Failure(GuestCountExceedsMax);
        }
        if (request.Message is { Length: > DealApplication.MessageMaxLength })
        {
            return Result<SubmitApplicationResult>.Failure(MessageTooLong);
        }

        // Predetermined-deposit flow: the tenant must consent to the Truth
        // Surface and provide a payment method up-front so host approval can
        // seal + charge atomically.
        if (featureFlags.BookingFlowV2Enabled)
        {
            if (!request.TruthSurfaceConsentGiven)
            {
                return Result<SubmitApplicationResult>.Failure(ConsentRequired);
            }
            if (string.IsNullOrWhiteSpace(request.StripePaymentMethodId))
            {
                return Result<SubmitApplicationResult>.Failure(PaymentMethodRequired);
            }
        }

        var duration = request.RequestedCheckOut.DayNumber - request.RequestedCheckIn.DayNumber;

        if (listing.MinStayDays.HasValue && duration < listing.MinStayDays.Value ||
            listing.MaxStayDays.HasValue && duration > listing.MaxStayDays.Value)
        {
            return Result<SubmitApplicationResult>.Failure(DatesOutOfRange);
        }

        var isAvailable = await listingProvider
            .IsAvailableAsync(request.ListingId, request.RequestedCheckIn, request.RequestedCheckOut, cancellationToken)
            .ConfigureAwait(false);
        if (!isAvailable)
        {
            return Result<SubmitApplicationResult>.Failure(DatesUnavailable);
        }

        // One live request per tenant per overlapping window. Other tenants may
        // still request the same/overlapping dates (only an active booking blocks
        // that, via IsAvailableAsync above), but the same tenant stacking
        // duplicate requests for dates they're already in the queue for — or have
        // already booked — is not allowed. Statuses that still represent a live
        // request/booking: Pending (awaiting host), Approved, and PaymentFailed
        // (sealed, awaiting a retry). Rejected/Cancelled/Expired don't count, so a
        // tenant can re-request after one of those.
        var hasOverlappingActiveRequest = await dbContext.DealApplications
            .AnyAsync(
                a => a.ListingId == request.ListingId
                  && a.TenantUserId == request.TenantUserId
                  && (a.Status == DealApplicationStatus.Pending
                      || a.Status == DealApplicationStatus.Approved
                      || a.Status == DealApplicationStatus.PaymentFailed)
                  && a.RequestedCheckIn < request.RequestedCheckOut
                  && request.RequestedCheckIn < a.RequestedCheckOut,
                cancellationToken)
            .ConfigureAwait(false);
        if (hasOverlappingActiveRequest)
        {
            return Result<SubmitApplicationResult>.Failure(DuplicateActiveRequest);
        }

        // Resolve the tenant's verification tier, select the predetermined
        // deposit for that tier, and quote fees — all snapshotted on the
        // application so the price can't drift before host approval.
        var pricing = await reservationPricingService
            .ComputeAsync(listing, request.TenantUserId, duration, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var tenantConsent = request.TruthSurfaceConsentGiven
            ? new TruthSurfaceConsentInput(
                true,
                request.ConsentVersion ?? BookingConsent.CurrentVersion,
                request.IpAddress,
                request.UserAgent)
            : null;

        var application = DealApplication.Submit(
            request.ListingId,
            request.TenantUserId,
            listing.LandlordUserId,
            request.RequestedCheckIn,
            request.RequestedCheckOut,
            guestCount: request.GuestCount,
            message: request.Message,
            stripePaymentMethodId: request.StripePaymentMethodId,
            depositSnapshot: pricing.ToSnapshot(),
            tenantConsent: tenantConsent);

        OwnerTenancyConsent.ApplyIfRequired(application, listing);

        dbContext.DealApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditTrail.RecordAsync(
            request.TenantUserId,
            "booking.reservation_requested",
            "Application",
            application.Id.ToString(),
            FormatRequestAudit(application),
            request.IpAddress,
            cancellationToken).ConfigureAwait(false);

        // Instant booking = implicit host pre-approval. Route through the same
        // atomic approval command so the Truth Surface seals (both consents)
        // and the off-session charge runs immediately, exactly as a manual
        // host approval would. Falls back silently to request-to-book if any
        // pre-condition fails.
        var instantBooked = false;
        DealApplicationDto applicationDto = DealApplicationDtoMapper.ToDto(application);
        if (listing.InstantBookingEnabled
            && !OwnerTenancyConsent.IsRequired(listing)
            && featureFlags.BookingFlowV2Enabled
            && await HostHasPayoutsAsync(listing.LandlordUserId, cancellationToken).ConfigureAwait(false))
        {
            var approveResult = await mediator.Send(
                new ApproveDealApplicationCommand(
                    application.Id,
                    listing.LandlordUserId,
                    TruthSurfaceConsentGiven: true,
                    ConsentVersion: BookingConsent.InstantBookHostVersion),
                cancellationToken).ConfigureAwait(false);

            if (approveResult.IsSuccess)
            {
                instantBooked = true;
                applicationDto = approveResult.Value;
            }
            else
            {
                LogInstantBookApproveFailed(logger, application.Id, approveResult.Error.Code);
            }
        }

        var nextPath = (instantBooked, applicationDto.DealId) switch
        {
            (true, { } d) => $"/app/deals/{d}/checkout",
            _ => $"/app/applications/{application.Id}",
        };

        return Result<SubmitApplicationResult>.Success(
            new SubmitApplicationResult(applicationDto, nextPath));
    }

    // Non-custodial (Option A): instant-book can only auto-charge when the host
    // has a Stripe Connect account with charges + payouts enabled, so the
    // destination charge settles straight to the host. Otherwise fall back to
    // request-to-book and let the host finish onboarding.
    private async Task<bool> HostHasPayoutsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var connectAccount = await hostStripeAccountProvider
            .GetByHostUserIdAsync(hostUserId, cancellationToken)
            .ConfigureAwait(false);
        return connectAccount is { ChargesEnabled: true, PayoutsEnabled: true };
    }

    private static string FormatRequestAudit(DealApplication application) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            listingId = application.ListingId,
            tier = application.TenantVerificationTierAtRequest?.ToString(),
            depositCents = application.DepositAmountCents,
            depositReason = application.DepositReason,
            serviceFeeCents = application.ServiceFeeCents,
            totalPayableCents = application.TotalPayableSnapshotCents,
            tenantConsentGiven = application.TenantTruthSurfaceConsentGiven,
            tenantConsentVersion = application.TenantConsentVersion,
            paymentMethodProvided = !string.IsNullOrEmpty(application.StripePaymentMethodId),
        });

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Instant-book approval failed for application {ApplicationId}: {ErrorCode}; left as request-to-book")]
    private static partial void LogInstantBookApproveFailed(
        ILogger logger, Guid applicationId, string errorCode);
}
