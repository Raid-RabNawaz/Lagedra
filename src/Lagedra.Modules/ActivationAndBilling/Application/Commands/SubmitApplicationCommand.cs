using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.TruthSurface.Application.Commands;
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
    string? StripePaymentMethodId = null) : IRequest<Result<SubmitApplicationResult>>;

/// <summary>
/// Phase 16 expanded result: when instant booking is enabled (and the
/// BookingFlow.V2 flag is on, and the host has payouts configured), the
/// command auto-approves the application and returns the new
/// <see cref="DealApplicationDto.DealId"/>. <see cref="NextPath"/> is a
/// frontend-relative route the UI uses to send the guest to the right
/// next screen — checkout for instant book, applications page for
/// request-to-book.
/// </summary>
public sealed record SubmitApplicationResult(
    DealApplicationDto Application,
    string NextPath);

public sealed partial class SubmitApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IInsuranceFeeCalculator insuranceFeeCalculator,
    IHostPaymentDetailsProvider hostPaymentDetailsProvider,
    IHostStripeAccountProvider hostStripeAccountProvider,
    IFeatureFlags featureFlags,
    IMediator mediator,
    IInquiryDealLinker inquiryDealLinker,
    ILogger<SubmitApplicationCommandHandler> logger)
    : IRequestHandler<SubmitApplicationCommand, Result<SubmitApplicationResult>>
{
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error DatesOutOfRange = new("Dates.OutOfStayRange", "Requested dates fall outside the listing's allowed stay range.");
    private static readonly Error DatesUnavailable = new("Dates.Unavailable", "The requested dates are not available.");
    private static readonly Error OwnListing = new("Application.OwnListing", "You cannot apply to your own listing.");
    private static readonly Error GuestCountInvalid = new(
        "Application.GuestCountInvalid", "Guest count must be at least 1.");
    private static readonly Error GuestCountExceedsMax = new(
        "Application.GuestCountExceedsMax",
        "Requested guest count exceeds the listing's maximum guests.");
    private static readonly Error MessageTooLong = new(
        "Application.MessageTooLong",
        $"Message must be {DealApplication.MessageMaxLength} characters or fewer.");

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

        // Validate the headcount/message pair as Result errors so the API
        // returns a friendly 400 instead of a 500 from the aggregate's
        // ArgumentOutOfRangeException. The aggregate's own checks remain
        // as a defence-in-depth backstop for non-API callers.
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

        var application = DealApplication.Submit(
            request.ListingId,
            request.TenantUserId,
            listing.LandlordUserId,
            request.RequestedCheckIn,
            request.RequestedCheckOut,
            guestCount: request.GuestCount,
            message: request.Message,
            stripePaymentMethodId: request.StripePaymentMethodId);

        // Phase 16: if the listing offers instant booking, the V2 flag is on,
        // and the host has a payout channel ready, auto-approve in the same
        // unit-of-work. Falls back silently to standard request-to-book if
        // any pre-condition fails — the application still lands in the host's
        // inbox, just without a deal id.
        var instantBooked = false;
        if (listing.InstantBookingEnabled
            && featureFlags.BookingFlowV2Enabled
            && await HostHasPayoutsAsync(listing.LandlordUserId, cancellationToken).ConfigureAwait(false))
        {
            var depositCents = listing.DefaultDepositCents ?? listing.MaxDepositCents;
            var insuranceQuote = await insuranceFeeCalculator
                .CalculateFeeAsync(listing.MonthlyRentCents, application.StayDurationDays, cancellationToken)
                .ConfigureAwait(false);
            var warning = JurisdictionWarningService.CheckForWarnings(
                listing.JurisdictionCode, application.StayDurationDays);

            application.Approve(
                depositCents,
                insuranceQuote.FeeCents,
                listing.MonthlyRentCents,
                warning);
            instantBooked = true;
        }

        dbContext.DealApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Phase 16.4: instant book auto-approves above; mirror the
        // ApproveDealApplicationCommand flow so the resulting checkout
        // surface already shows a landlord-confirmed Truth Surface and
        // only the tenant tick-and-pay remains.
        //
        // The off-session card-on-file charge (16.9) intentionally
        // does NOT run here. Even with instant book + saved card, the
        // tenant must inline-confirm the Truth Surface on /checkout
        // first — the snapshot is a hard architectural gate. Once the
        // tenant ticks confirm, OnTruthSurfaceConfirmedCreatePayment-
        // ConfirmationHandler creates the confirmation row and (when a
        // payment method is on file) charges off-session, which then
        // raises PaymentConfirmedEvent and activates the deal.
        if (instantBooked && application.DealId is { } instantDealId)
        {
            // Phase 17: preserve the tenant's pre-booking inquiry thread by
            // linking it to the freshly-minted deal id. No-op when the
            // tenant never started a thread; idempotent if they did.
            await inquiryDealLinker
                .LinkOpenInquiryToDealAsync(
                    application.ListingId,
                    application.TenantUserId,
                    instantDealId,
                    cancellationToken)
                .ConfigureAwait(false);

            await AutoConfirmTruthSurfaceAsync(
                instantDealId,
                application.LandlordUserId,
                application.Id,
                cancellationToken).ConfigureAwait(false);
        }

        var nextPath = (instantBooked, application.DealId) switch
        {
            (true, { } d) => $"/app/deals/{d}/checkout",
            _ => $"/app/applications/{application.Id}",
        };

        return Result<SubmitApplicationResult>.Success(
            new SubmitApplicationResult(MapToDto(application), nextPath));
    }

    private async Task AutoConfirmTruthSurfaceAsync(
        Guid dealId,
        Guid landlordUserId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var createResult = await mediator
            .Send(new CreateTruthSurfaceForDealCommand(dealId, landlordUserId), cancellationToken)
            .ConfigureAwait(false);

        if (!createResult.IsSuccess)
        {
            LogTruthSurfaceCreateFailed(logger, applicationId, dealId, createResult.Error.Code);
            return;
        }

        var snapshotId = createResult.Value.SnapshotId;
        var confirmResult = await mediator
            .Send(
                new ConfirmTruthSurfaceCommand(snapshotId, ConfirmingParty.Landlord),
                cancellationToken)
            .ConfigureAwait(false);

        if (!confirmResult.IsSuccess)
        {
            LogTruthSurfaceLandlordConfirmFailed(logger, applicationId, snapshotId, confirmResult.Error.Code);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auto-create Truth Surface failed for instant-book application {ApplicationId} deal {DealId}: {ErrorCode}")]
    private static partial void LogTruthSurfaceCreateFailed(
        ILogger logger, Guid applicationId, Guid dealId, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auto-confirm Truth Surface (landlord) failed for instant-book application {ApplicationId} snapshot {SnapshotId}: {ErrorCode}")]
    private static partial void LogTruthSurfaceLandlordConfirmFailed(
        ILogger logger, Guid applicationId, Guid snapshotId, string errorCode);

    private async Task<bool> HostHasPayoutsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var directPayout = await hostPaymentDetailsProvider
            .GetDecryptedPaymentDetailsAsync(hostUserId, cancellationToken)
            .ConfigureAwait(false);
        if (directPayout is not null)
        {
            return true;
        }

        var connectAccount = await hostStripeAccountProvider
            .GetByHostUserIdAsync(hostUserId, cancellationToken)
            .ConfigureAwait(false);
        return connectAccount is { ChargesEnabled: true, PayoutsEnabled: true };
    }

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source,
            a.GuestCount, a.Message);
}
