using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.TruthSurface.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ApproveDealApplicationCommand(
    Guid ApplicationId,
    Guid CallerUserId,
    long DepositAmountCents) : IRequest<Result<DealApplicationDto>>;

public sealed partial class ApproveDealApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IInsuranceFeeCalculator insuranceFeeCalculator,
    IMediator mediator,
    IFeatureFlags featureFlags,
    IInquiryDealLinker inquiryDealLinker,
    ILogger<ApproveDealApplicationCommandHandler> logger)
    : IRequestHandler<ApproveDealApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error ApplicationNotFound = new("Application.NotFound", "Application not found.");
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Associated listing not found.");
    private static readonly Error Forbidden = new("Application.Forbidden", "You do not own the listing for this application.");
    private static readonly Error DatesUnavailable = new("Dates.Unavailable", "The requested dates are no longer available.");

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

        var listing = await listingProvider
            .GetListingDetailsAsync(application.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<DealApplicationDto>.Failure(ListingNotFound);
        }

        var isAvailable = await listingProvider
            .IsAvailableAsync(application.ListingId, application.RequestedCheckIn, application.RequestedCheckOut, cancellationToken)
            .ConfigureAwait(false);
        if (!isAvailable)
        {
            return Result<DealApplicationDto>.Failure(DatesUnavailable);
        }

        if (request.DepositAmountCents > listing.MaxDepositCents)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Deposit.ExceedsMax",
                    $"Deposit ({request.DepositAmountCents}) exceeds listing max ({listing.MaxDepositCents})."));
        }

        var quote = await insuranceFeeCalculator
            .CalculateFeeAsync(listing.MonthlyRentCents, application.StayDurationDays, cancellationToken)
            .ConfigureAwait(false);

        var warning = JurisdictionWarningService.CheckForWarnings(
            listing.JurisdictionCode, application.StayDurationDays);

        application.Approve(
            request.DepositAmountCents,
            quote.FeeCents,
            listing.MonthlyRentCents,
            warning);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Phase 16.4: collapse the host's three actions (approve → create
        // truth surface → confirm truth surface) into a single click. We
        // dispatch the two TruthSurface commands sequentially via MediatR
        // so the deal lands on the tenant's checkout with a landlord-
        // confirmed snapshot already waiting for them.
        //
        // The off-session card-on-file charge (16.9) intentionally does
        // NOT run here. The Truth Surface is a hard architectural gate:
        // the tenant must inline-confirm the snapshot first. The actual
        // charge fires from OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler
        // once the snapshot seals — that's the *only* path that can
        // produce a Confirmed DealPaymentConfirmation under V2.
        if (application.DealId is { } dealId)
        {
            // Phase 17: link the tenant's pre-booking inquiry thread (if
            // any) onto the freshly-created deal so the conversation
            // history surfaces on the deal page.
            await inquiryDealLinker
                .LinkOpenInquiryToDealAsync(
                    application.ListingId,
                    application.TenantUserId,
                    dealId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (featureFlags.BookingFlowV2Enabled)
            {
                await AutoConfirmTruthSurfaceAsync(
                    dealId,
                    application.LandlordUserId,
                    application.Id,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return Result<DealApplicationDto>.Success(MapToDto(application));
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
            // The host can still create + confirm manually from the deal
            // detail page if this best-effort step fails (e.g. retry).
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

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source,
            a.GuestCount, a.Message);

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
