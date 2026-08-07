using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.TruthSurface.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Host accepts a reservation request. No deposit input — the predetermined
/// deposit + fees were snapshotted at request time. This is the single atomic
/// step that records the host's Truth Surface consent, seals the agreement
/// (both consents), and lets the off-session charge + activation run. Idempotent:
/// a repeat call on an already-approved application returns the existing booking
/// without charging again.
/// </summary>
public sealed record ApproveDealApplicationCommand(
    Guid ApplicationId,
    Guid CallerUserId,
    bool TruthSurfaceConsentGiven = true,
    string? ConsentVersion = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<DealApplicationDto>>;

public sealed partial class ApproveDealApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IMediator mediator,
    IFeatureFlags featureFlags,
    IInquiryDealLinker inquiryDealLinker,
    IHostStripeAccountProvider hostStripeAccountProvider,
    IAuditTrailWriter auditTrail,
    ILogger<ApproveDealApplicationCommandHandler> logger)
    : IRequestHandler<ApproveDealApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error ApplicationNotFound = new("Application.NotFound", "Application not found.");
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Associated listing not found.");
    private static readonly Error Forbidden = new("Application.Forbidden", "You do not own the listing for this application.");
    private static readonly Error DatesUnavailable = new("Dates.Unavailable", "The requested dates are no longer available.");
    private static readonly Error ConsentRequired = new("Application.HostConsentRequired", "You must agree to the Truth Surface terms to accept this request.");
    private static readonly Error NotApprovable = new("Application.NotApprovable", "This request can no longer be accepted.");
    private static readonly Error NoDepositSnapshot = new("Application.NoDepositSnapshot", "This request has no deposit snapshot and cannot be accepted under the predetermined-deposit flow.");
    private static readonly Error HostPayoutSetupRequired = new(
        "Application.HostPayoutSetupRequired",
        "Add your payout details before accepting this request. Accepting charges the tenant's deposit and first payment immediately, which needs a payout destination. Complete payout setup, then accept again.");
    private static readonly Error PaymentNotReady = new(
        "Application.PaymentNotReady",
        "This request is not ready to accept yet. The tenant must complete payment authorization and Truth Surface consent first.");
    private static readonly Error PreciseAddressRequired = new(
        "Application.PreciseAddressRequired",
        "Lock the full property address on this listing before accepting a request. The address is required for the lease agreement and stay details.");

    public async Task<Result<DealApplicationDto>> Handle(
        ApproveDealApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<DealApplicationDto>.Failure(ApplicationNotFound);
        }

        if (application.LandlordUserId != request.CallerUserId)
        {
            return Result<DealApplicationDto>.Failure(Forbidden);
        }

        // Idempotency: a request that's already been accepted (Approved, or
        // Approved-but-payment-failed) returns the existing booking without
        // re-sealing or re-charging.
        if (application.Status is DealApplicationStatus.Approved or DealApplicationStatus.PaymentFailed)
        {
            return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
        }

        if (application.Status != DealApplicationStatus.Pending)
        {
            return Result<DealApplicationDto>.Failure(NotApprovable);
        }

        if (!request.TruthSurfaceConsentGiven)
        {
            return Result<DealApplicationDto>.Failure(ConsentRequired);
        }

        if (application.DepositAmountCents is null)
        {
            return Result<DealApplicationDto>.Failure(NoDepositSnapshot);
        }

        // Partner-direct (and any V2 card-on-file) bookings must be payment-ready
        // before the host can approve: PM on file + tenant Truth Surface consent.
        if (featureFlags.BookingFlowV2Enabled
            && (application.Source == DealApplicationSource.PartnerDirectReservation
                || !string.IsNullOrEmpty(application.StripePaymentMethodId))
            && !application.IsPaymentReady)
        {
            return Result<DealApplicationDto>.Failure(PaymentNotReady);
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(application.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<DealApplicationDto>.Failure(ListingNotFound);
        }

        if (listing.PreciseAddress is null
            || string.IsNullOrWhiteSpace(listing.PreciseAddress.Street)
            || string.IsNullOrWhiteSpace(listing.PreciseAddress.City))
        {
            return Result<DealApplicationDto>.Failure(PreciseAddressRequired);
        }

        var isAvailable = await listingProvider
            .IsAvailableAsync(application.ListingId, application.RequestedCheckIn, application.RequestedCheckOut, cancellationToken)
            .ConfigureAwait(false);
        if (!isAvailable)
        {
            return Result<DealApplicationDto>.Failure(DatesUnavailable);
        }

        // Under V2 the host's acceptance immediately seals the Truth Surface and
        // fires the off-session charge of the predetermined deposit + first
        // payment. That charge (and any tenant retry at checkout) needs a payout
        // destination, so the host must have completed payout setup first.
        // Fail early with a clear message instead of letting the booking fall
        // into PaymentFailed and stranding the tenant at a checkout dead-end.
        if (featureFlags.BookingFlowV2Enabled
            && !string.IsNullOrEmpty(application.StripePaymentMethodId)
            && !await HostHasPayoutsAsync(application.LandlordUserId, cancellationToken).ConfigureAwait(false))
        {
            return Result<DealApplicationDto>.Failure(HostPayoutSetupRequired);
        }

        var warning = JurisdictionWarningService.CheckForWarnings(
            listing.JurisdictionCode, application.StayDurationDays);

        var hostConsent = new TruthSurfaceConsentInput(
            true,
            request.ConsentVersion ?? BookingConsent.CurrentVersion,
            request.IpAddress,
            request.UserAgent);

        var dealId = application.Approve(warning, hostConsent);

        // Accepting this request claims its dates. Any other still-pending
        // request for the same listing whose stay overlaps can no longer be
        // honoured (the dates are now taken), so auto-reject them in the same
        // transaction. Each raises ApplicationSupersededEvent so those tenants
        // are told their dates were booked by someone else rather than that the
        // host declined them. Non-overlapping pending requests are left alone.
        var supersededCount = 0;
        var overlapping = await dbContext.DealApplications
            .Where(a => a.ListingId == application.ListingId
                     && a.Id != application.Id
                     && a.Status == DealApplicationStatus.Pending
                     && a.RequestedCheckIn < application.RequestedCheckOut
                     && application.RequestedCheckIn < a.RequestedCheckOut)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var other in overlapping)
        {
            other.RejectAsSuperseded();
            supersededCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (supersededCount > 0)
        {
            LogSupersededOverlapping(logger, supersededCount, application.ListingId, application.Id);
        }

        await auditTrail.RecordAsync(
            request.CallerUserId,
            "booking.approved",
            "Deal",
            dealId.ToString(),
            $"{{\"applicationId\":\"{application.Id}\",\"hostConsentVersion\":\"{application.HostConsentVersion}\"}}",
            request.IpAddress,
            cancellationToken).ConfigureAwait(false);

        // Phase 17: link the tenant's pre-booking inquiry thread (if any) onto
        // the freshly-created deal so the conversation history surfaces.
        await inquiryDealLinker
            .LinkOpenInquiryToDealAsync(
                application.ListingId,
                application.TenantUserId,
                dealId,
                cancellationToken)
            .ConfigureAwait(false);

        if (featureFlags.BookingFlowV2Enabled)
        {
            await SealTruthSurfaceAsync(application, cancellationToken).ConfigureAwait(false);
        }

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }

    // Mirrors the precondition CardOnFileChargeService enforces at charge time:
    // non-custodial (Option A) requires a Stripe Connect account with charges +
    // payouts enabled so the destination charge can settle straight to the host.
    // Keeping this in lock-step means a passing approval will actually be chargeable.
    private async Task<bool> HostHasPayoutsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var connectAccount = await hostStripeAccountProvider
            .GetByHostUserIdAsync(hostUserId, cancellationToken)
            .ConfigureAwait(false);
        return connectAccount is { ChargesEnabled: true, PayoutsEnabled: true };
    }

    private async Task SealTruthSurfaceAsync(DealApplication application, CancellationToken cancellationToken)
    {
        if (application.DealId is not { } dealId)
        {
            return;
        }

        // New flow: the tenant already consented at request time, so seal the
        // Truth Surface with BOTH consents in one step. This raises
        // TruthSurfaceConfirmedEvent → off-session charge → activation.
        if (application.TenantTruthSurfaceConsentGiven && application.HostTruthSurfaceConsentGiven)
        {
            var sealResult = await mediator.Send(
                new CreateAndSealTruthSurfaceCommand(
                    dealId,
                    application.TenantUserId,
                    application.TenantTruthSurfaceConsentAt ?? DateTime.UtcNow,
                    application.TenantConsentIpAddress,
                    application.TenantConsentUserAgent,
                    application.TenantConsentVersion ?? BookingConsent.CurrentVersion,
                    application.LandlordUserId,
                    application.HostTruthSurfaceConsentAt ?? DateTime.UtcNow,
                    application.HostConsentIpAddress,
                    application.HostConsentUserAgent,
                    application.HostConsentVersion ?? BookingConsent.CurrentVersion),
                cancellationToken).ConfigureAwait(false);

            if (!sealResult.IsSuccess)
            {
                LogTruthSurfaceSealFailed(logger, application.Id, dealId, sealResult.Error.Code);
            }
            else
            {
                await auditTrail.RecordAsync(
                    application.LandlordUserId,
                    "truth_surface.locked",
                    "TruthSurface",
                    sealResult.Value.SnapshotId.ToString(),
                    $"{{\"dealId\":\"{dealId}\"}}",
                    ct: cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        // Legacy / partner-direct path (no tenant pre-consent): create the
        // snapshot and confirm as landlord only; the tenant confirms + pays at
        // checkout, exactly as before.
        var createResult = await mediator
            .Send(new CreateTruthSurfaceForDealCommand(dealId, application.LandlordUserId), cancellationToken)
            .ConfigureAwait(false);

        if (!createResult.IsSuccess)
        {
            LogTruthSurfaceCreateFailed(logger, application.Id, dealId, createResult.Error.Code);
            return;
        }

        var confirmResult = await mediator
            .Send(
                new ConfirmTruthSurfaceCommand(createResult.Value.SnapshotId, ConfirmingParty.Landlord),
                cancellationToken)
            .ConfigureAwait(false);

        if (!confirmResult.IsSuccess)
        {
            LogTruthSurfaceLandlordConfirmFailed(logger, application.Id, createResult.Value.SnapshotId, confirmResult.Error.Code);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto-rejected {Count} overlapping pending request(s) for listing {ListingId} after accepting application {ApplicationId}")]
    private static partial void LogSupersededOverlapping(
        ILogger logger, int count, Guid listingId, Guid applicationId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Atomic Truth Surface seal failed for application {ApplicationId} deal {DealId}: {ErrorCode}")]
    private static partial void LogTruthSurfaceSealFailed(
        ILogger logger, Guid applicationId, Guid dealId, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auto-create Truth Surface failed for application {ApplicationId} deal {DealId}: {ErrorCode}")]
    private static partial void LogTruthSurfaceCreateFailed(
        ILogger logger, Guid applicationId, Guid dealId, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auto-confirm Truth Surface (landlord) failed for application {ApplicationId} snapshot {SnapshotId}: {ErrorCode}")]
    private static partial void LogTruthSurfaceLandlordConfirmFailed(
        ILogger logger, Guid applicationId, Guid snapshotId, string errorCode);
}
