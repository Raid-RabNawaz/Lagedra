using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record SubmitApplicationCommand(
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut) : IRequest<Result<DealApplicationDto>>;

public sealed class SubmitApplicationCommandHandler(
    BillingDbContext dbContext,
    ListingsDbContext listingsDbContext)
    : IRequestHandler<SubmitApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error DatesOutOfRange = new("Dates.OutOfStayRange", "Requested dates fall outside the listing's allowed stay range.");
    private static readonly Error DatesUnavailable = new("Dates.Unavailable", "The requested dates are not available.");

    public async Task<Result<DealApplicationDto>> Handle(
        SubmitApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await listingsDbContext.Listings
            .AsNoTracking()
            .Include(l => l.AvailabilityBlocks)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<DealApplicationDto>.Failure(ListingNotFound);
        }

        var duration = request.RequestedCheckOut.DayNumber - request.RequestedCheckIn.DayNumber;

        if (listing.StayRange is not null &&
            (duration < listing.StayRange.MinDays || duration > listing.StayRange.MaxDays))
        {
            return Result<DealApplicationDto>.Failure(DatesOutOfRange);
        }

        if (!AvailabilityService.IsAvailable(listing.AvailabilityBlocks, request.RequestedCheckIn, request.RequestedCheckOut))
        {
            return Result<DealApplicationDto>.Failure(DatesUnavailable);
        }

        var application = DealApplication.Submit(
            request.ListingId,
            request.TenantUserId,
            request.LandlordUserId,
            request.RequestedCheckIn,
            request.RequestedCheckOut);

        dbContext.DealApplications.Add(application);
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
