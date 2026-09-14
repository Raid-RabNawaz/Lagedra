using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Queries;

public sealed record GetAllViolationsQuery : IRequest<Result<IReadOnlyList<ViolationDto>>>;

public sealed class GetAllViolationsQueryHandler(
    ComplianceDbContext dbContext,
    IUserLookupService userLookup,
    IDealApplicationStatusProvider dealProvider,
    IListingProvider listingProvider)
    : IRequestHandler<GetAllViolationsQuery, Result<IReadOnlyList<ViolationDto>>>
{
    public async Task<Result<IReadOnlyList<ViolationDto>>> Handle(
        GetAllViolationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await dbContext.Violations
            .AsNoTracking()
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<ViolationDto>(violations.Count);
        foreach (var v in violations)
        {
            var target = await userLookup
                .FindAccountByIdAsync(v.TargetUserId, cancellationToken)
                .ConfigureAwait(false);
            var reporter = await userLookup
                .FindAccountByIdAsync(v.ReportedByUserId, cancellationToken)
                .ConfigureAwait(false);
            var deal = await dealProvider
                .GetDealDetailsAsync(v.DealId, cancellationToken)
                .ConfigureAwait(false);
            string? listingTitle = null;
            if (deal is not null)
            {
                var listing = await listingProvider
                    .GetListingDetailsAsync(deal.ListingId, cancellationToken)
                    .ConfigureAwait(false);
                listingTitle = listing?.Title;
            }

            items.Add(new ViolationDto(
                v.Id, v.DealId, v.ReportedByUserId, v.TargetUserId, v.Category, v.Status,
                v.Description, v.EvidenceReference, v.DetectedAt, v.ResolvedAt,
                target?.Email, target?.DisplayName,
                reporter?.Email, reporter?.DisplayName,
                deal?.ListingId, listingTitle));
        }

        return Result<IReadOnlyList<ViolationDto>>.Success(items);
    }
}
