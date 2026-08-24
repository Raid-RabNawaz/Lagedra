using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record ListOwnerPendingApplicationsQuery(Guid OwnerUserId)
    : IRequest<Result<IReadOnlyList<DealApplicationDto>>>;

public sealed class ListOwnerPendingApplicationsQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider)
    : IRequestHandler<ListOwnerPendingApplicationsQuery, Result<IReadOnlyList<DealApplicationDto>>>
{
    public async Task<Result<IReadOnlyList<DealApplicationDto>>> Handle(
        ListOwnerPendingApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var applications = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.HomeOwnerUserId == request.OwnerUserId
                        && a.OwnerConsentRequired
                        && !a.OwnerTenancyConsentGiven
                        && !a.OwnerTenancyConsentDeclined
                        && a.Status == DealApplicationStatus.Pending)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var listingIds = applications.Select(a => a.ListingId).Distinct().ToList();
        var summaries = listingIds.Count == 0
            ? Array.Empty<ListingSummaryInfoDto>()
            : await listingProvider
                .GetListingSummariesAsync(listingIds, cancellationToken)
                .ConfigureAwait(false);
        var summaryById = summaries.ToDictionary(s => s.Id);

        IReadOnlyList<DealApplicationDto> dtos = applications
            .Select(a =>
            {
                var dto = DealApplicationDtoMapper.ToDto(a);
                return summaryById.TryGetValue(a.ListingId, out var summary)
                    ? dto with
                    {
                        ListingTitle = summary.Title,
                        ListingCoverPhotoUri = summary.CoverPhotoUri,
                        ListingCity = summary.City,
                    }
                    : dto;
            })
            .ToList();

        return Result<IReadOnlyList<DealApplicationDto>>.Success(dtos);
    }
}
