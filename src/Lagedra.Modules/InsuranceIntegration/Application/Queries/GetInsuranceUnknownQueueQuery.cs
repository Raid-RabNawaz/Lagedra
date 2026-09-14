using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Domain.Policies;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.Queries;

public sealed record GetInsuranceUnknownQueueQuery : IRequest<Result<IReadOnlyList<InsuranceQueueItemDto>>>;

public sealed class GetInsuranceUnknownQueueQueryHandler(
    InsuranceDbContext dbContext,
    IUserLookupService userLookup,
    IDealApplicationStatusProvider dealProvider,
    IListingProvider listingProvider)
    : IRequestHandler<GetInsuranceUnknownQueueQuery, Result<IReadOnlyList<InsuranceQueueItemDto>>>
{
    public async Task<Result<IReadOnlyList<InsuranceQueueItemDto>>> Handle(
        GetInsuranceUnknownQueueQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var utcNow = DateTime.UtcNow;

        var records = await dbContext.PolicyRecords
            .AsNoTracking()
            .Where(r => r.State == InsuranceState.Unknown && r.UnknownSince != null)
            .OrderBy(r => r.UnknownSince)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<InsuranceQueueItemDto>(records.Count);
        foreach (var record in records)
        {
            var elapsed = (utcNow - record.UnknownSince!.Value).TotalHours;
            var remaining = Math.Max(0, UnknownGraceWindowPolicy.GraceHours - elapsed);
            var account = await userLookup
                .FindAccountByIdAsync(record.TenantUserId, cancellationToken)
                .ConfigureAwait(false);
            var deal = await dealProvider
                .GetDealDetailsAsync(record.DealId, cancellationToken)
                .ConfigureAwait(false);
            string? listingTitle = null;
            if (deal is not null)
            {
                var listing = await listingProvider
                    .GetListingDetailsAsync(deal.ListingId, cancellationToken)
                    .ConfigureAwait(false);
                listingTitle = listing?.Title;
            }

            items.Add(new InsuranceQueueItemDto(
                record.Id,
                record.DealId,
                record.TenantUserId,
                record.UnknownSince.Value,
                remaining,
                account?.Email,
                account?.DisplayName,
                deal?.ListingId,
                listingTitle,
                deal?.RequestedCheckIn,
                deal?.RequestedCheckOut,
                record.Provider,
                record.PolicyNumber,
                record.CoverageScope,
                record.ScreeningStatus,
                record.FlaggedReason,
                record.ExpiresAt,
                record.VerifiedAt));
        }

        return Result<IReadOnlyList<InsuranceQueueItemDto>>.Success(items);
    }
}
