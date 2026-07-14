using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
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
    IListingProvider listingProvider,
    IPartnerOrganizationBillingProfile partnerOrgBilling)
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

                return DealApplicationDtoMapper.ToDto(a, partnerName);
            })
            .ToList();

        return Result<IReadOnlyList<DealApplicationDto>>.Success(dtos);
    }
}
