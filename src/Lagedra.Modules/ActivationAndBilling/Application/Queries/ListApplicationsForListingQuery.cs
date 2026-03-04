using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record ListApplicationsForListingQuery(
    Guid ListingId) : IRequest<Result<IReadOnlyList<DealApplicationDto>>>;

public sealed class ListApplicationsForListingQueryHandler(
    BillingDbContext dbContext)
    : IRequestHandler<ListApplicationsForListingQuery, Result<IReadOnlyList<DealApplicationDto>>>
{
    public async Task<Result<IReadOnlyList<DealApplicationDto>>> Handle(
        ListApplicationsForListingQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning);
}
