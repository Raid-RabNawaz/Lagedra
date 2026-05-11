using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record ListApplicationsForListingQuery(
    Guid ListingId,
    Guid CallerUserId) : IRequest<Result<IReadOnlyList<DealApplicationDto>>>;

public sealed class ListApplicationsForListingQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider)
    : IRequestHandler<ListApplicationsForListingQuery, Result<IReadOnlyList<DealApplicationDto>>>
{
    private static readonly Error Forbidden = new("Application.Forbidden", "You do not own this listing.");
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<IReadOnlyList<DealApplicationDto>>> Handle(
        ListApplicationsForListingQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Always verify ownership against the listing itself, not just the first application.
        // An empty application list previously leaked an empty-but-successful response to
        // any caller; the role merge means any Member could probe listings this way.
        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<IReadOnlyList<DealApplicationDto>>.Failure(NotFound);
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<IReadOnlyList<DealApplicationDto>>.Failure(Forbidden);
        }

        var applications = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.ListingId == request.ListingId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<DealApplicationDto> dtos = applications
            .Select(MapToDto)
            .ToList();

        return Result<IReadOnlyList<DealApplicationDto>>.Success(dtos);
    }

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source);
}
