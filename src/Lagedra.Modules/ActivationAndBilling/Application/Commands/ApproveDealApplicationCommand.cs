using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ApproveDealApplicationCommand(
    Guid ApplicationId,
    long DepositAmountCents) : IRequest<Result<DealApplicationDto>>;

public sealed class ApproveDealApplicationCommandHandler(
    BillingDbContext dbContext,
    ListingsDbContext listingsDbContext,
    IInsuranceFeeCalculator insuranceFeeCalculator)
    : IRequestHandler<ApproveDealApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error ApplicationNotFound = new("Application.NotFound", "Application not found.");
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Associated listing not found.");
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

        var listing = await listingsDbContext.Listings
            .AsNoTracking()
            .Include(l => l.AvailabilityBlocks)
            .FirstOrDefaultAsync(l => l.Id == application.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<DealApplicationDto>.Failure(ListingNotFound);
        }

        if (!AvailabilityService.IsAvailable(
                listing.AvailabilityBlocks, application.RequestedCheckIn, application.RequestedCheckOut))
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

        return Result<DealApplicationDto>.Success(MapToDto(application));
    }

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning);
}
