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
    IListingProvider listingProvider,
    IPartnerOrganizationBillingProfile partnerOrgBilling)
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

        var partnerOrgIds = applications
            .Where(a => a.PartnerOrganizationId is not null)
            .Select(a => a.PartnerOrganizationId!.Value)
            .Distinct()
            .ToList();

        var partnerNames = new Dictionary<Guid, string?>();
        foreach (var orgId in partnerOrgIds)
        {
            partnerNames[orgId] = await partnerOrgBilling
                .GetNameAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<DealApplicationDto> dtos = applications
            .Select(a =>
            {
                string? partnerName = null;
                if (a.PartnerOrganizationId is { } orgId)
                {
                    partnerNames.TryGetValue(orgId, out partnerName);
                }

                var dto = DealApplicationDtoMapper.ToDto(a, partnerName);
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
