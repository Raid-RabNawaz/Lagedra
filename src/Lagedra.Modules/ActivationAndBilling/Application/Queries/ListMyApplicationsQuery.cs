using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record ListMyApplicationsQuery(
    Guid UserId) : IRequest<Result<IReadOnlyList<DealApplicationDto>>>;

public sealed class ListMyApplicationsQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider)
    : IRequestHandler<ListMyApplicationsQuery, Result<IReadOnlyList<DealApplicationDto>>>
{
    public async Task<Result<IReadOnlyList<DealApplicationDto>>> Handle(
        ListMyApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var applications = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.TenantUserId == request.UserId || a.LandlordUserId == request.UserId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Enrich each row with the listing's title / cover / city so inbox
        // cards can render the property identity. This matters most for the
        // tenant "my applications" view: the tenant doesn't own these listings
        // and so can't resolve them client-side (it previously fell back to a
        // bare "Property" placeholder).
        var listingIds = applications
            .Select(a => a.ListingId)
            .Distinct()
            .ToList();

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
